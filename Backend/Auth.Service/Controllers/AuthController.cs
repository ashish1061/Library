using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shared.Core.Domain;
using Shared.Core.DTOs;
using Shared.Core.Interfaces;
using Shared.Infrastructure.Repositories;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;
using Hangfire;

namespace Auth.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IEmailTemplateRepository _emailTemplateRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthController(
            IEmployeeRepository employeeRepository, 
            IConfiguration configuration, 
            IEmailService emailService, 
            IAuditLogRepository auditLogRepository, 
            IEmailTemplateRepository emailTemplateRepository,
            IOtpRepository otpRepository,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _employeeRepository = employeeRepository;
            _configuration = configuration;
            _emailService = emailService;
            _auditLogRepository = auditLogRepository;
            _emailTemplateRepository = emailTemplateRepository;
            _otpRepository = otpRepository;
            _refreshTokenRepository = refreshTokenRepository;
        }

        [HttpPost("login")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginRateLimiter")]
        public async Task<IActionResult> Login([FromBody] EncryptedLoginRequest encryptedRequest)
        {
            if (encryptedRequest == null || string.IsNullOrEmpty(encryptedRequest.EncryptedData))
            {
                return BadRequest(new { message = "Invalid request payload." });
            }

            string decryptedJson;
            try
            {
                decryptedJson = DecryptPayload(encryptedRequest.EncryptedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Decryption Error] Failed to decrypt login payload: {ex.Message}");
                return BadRequest(new { message = "Decryption failed. Please try again." });
            }

            LoginRequest request;
            try
            {
                request = JsonSerializer.Deserialize<LoginRequest>(decryptedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Deserialization Error] Failed to parse login payload: {ex.Message}");
                return BadRequest(new { message = "Malformed request payload." });
            }

            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email and Password are required." });
            }

            var employee = await _employeeRepository.GetEmployeeByEmailAsync(request.Email);

            if (employee == null || string.IsNullOrEmpty(employee.password)) 
            {
                return Unauthorized(new { message = $"Invalid email '{request.Email}' or password" });
            }

            if (string.IsNullOrEmpty(employee.EmpID) || string.IsNullOrEmpty(employee.emailid))
            {
                return Unauthorized(new { message = "Login disabled: Employee code and email ID are required." });
            }

            if (employee.LockoutEnd.HasValue && employee.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingMinutes = (int)Math.Ceiling((employee.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                return StatusCode(423, new { message = $"Account locked. Try again in {remainingMinutes} minutes." });
            }

            // Verify using BCrypt
            if (string.IsNullOrEmpty(employee.password) || !employee.password.StartsWith("$2"))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, employee.password);

            if (!isPasswordValid)
            {
                employee.FailedLoginAttempts += 1;
                if (employee.FailedLoginAttempts >= 5)
                {
                    employee.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    await _auditLogRepository.LogActionAsync("AccountLocked", employee.EmpID, "Employee", employee.EmpID, "Account locked due to 5 failed login attempts.");
                }
                await _employeeRepository.UpdateEmployeeAsync(employee);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (request.Password == "Library@123")
            {
                return StatusCode(403, new { requirePasswordChange = true, email = employee.emailid, message = "You must change your default password to continue." });
            }

            // If password is valid, reset attempts
            if (employee.FailedLoginAttempts > 0 || employee.LockoutEnd.HasValue)
            {
                employee.FailedLoginAttempts = 0;
                employee.LockoutEnd = null;
                await _employeeRepository.UpdateEmployeeAsync(employee);
            }

            // Check if MFA is enabled
            if (!employee.IsMfaEnabled)
            {
                await _auditLogRepository.LogActionAsync("Login", employee.EmpID, "Employee", employee.EmpID, "Logged in successfully (MFA disabled).");
                var tokenResponse = await GenerateJwtTokenAsync(employee);
                return Ok(tokenResponse);
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            await _otpRepository.SaveOtpAsync(employee.emailid, otp, DateTime.UtcNow.AddMinutes(5));

            Console.WriteLine($"[MFA OTP] Generated OTP for user {employee.emailid}: {otp}");
            await _auditLogRepository.LogActionAsync("MfaOtpGenerated", employee.EmpID, "Employee", employee.EmpID, $"OTP generated for email login. Debug OTP: {otp}");

            // Send OTP email via Hangfire background job
            var template = await _emailTemplateRepository.GetTemplateByPurposeAsync("Login OTP");
            var subject = template?.Subject ?? "Your Library Login OTP";
            var body = template?.Body ?? "Hello {{EmployeeName}},<br><br>Your one-time password (OTP) for logging into the Library System is: <b>{{OTP}}</b><br><br>This code is valid for 5 minutes.";

            subject = subject.Replace("{{EmployeeName}}", employee.EmpName).Replace("{{OTP}}", otp).Replace("{{otp}}", otp);
            body = body.Replace("{{EmployeeName}}", employee.EmpName).Replace("{{OTP}}", otp).Replace("{{otp}}", otp);

            BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(employee.emailid, subject, body));

            return Ok(new { mfaRequired = true, email = employee.emailid });
        }

        private string DecryptPayload(string encryptedText)
        {
            var key = _configuration["Encryption:Key"];
            var iv = _configuration["Encryption:IV"];
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
            {
                throw new InvalidOperationException("Encryption Key or IV is not configured in application configuration.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var ivBytes = Encoding.UTF8.GetBytes(iv);
            var cipherBytes = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        [HttpPost("verify-otp")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginRateLimiter")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var employee = await _employeeRepository.GetEmployeeByEmailAsync(request.Email);
            if (employee == null)
            {
                return BadRequest(new { message = "Employee not found." });
            }

            var cachedOtp = await _otpRepository.GetOtpAsync(request.Email);
            if (cachedOtp != null)
            {
                if (cachedOtp.Value.Expiry > DateTime.UtcNow && cachedOtp.Value.Otp == request.Otp)
                {
                    await _otpRepository.DeleteOtpAsync(request.Email);
                    
                    await _auditLogRepository.LogActionAsync("LoginMfaSuccess", employee.EmpID, "Employee", employee.EmpID, "MFA verification successful. Logged in.");
                    
                    var tokenResponse = await GenerateJwtTokenAsync(employee);
                    return Ok(tokenResponse);
                }
            }

            await _auditLogRepository.LogActionAsync("LoginMfaFailure", employee.EmpID, "Employee", employee.EmpID, $"MFA verification failed. Input OTP: {request.Otp}");
            return BadRequest(new { message = "Invalid or expired OTP." });
        }

        [HttpPost("register")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _employeeRepository.GetEmployeeByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already in use." });
            }

            var existingEmpId = await _employeeRepository.GetEmployeeByIdAsync(request.EmpID);
            if (existingEmpId != null)
            {
                return BadRequest(new { message = "Employee ID already exists." });
            }

            var employee = new Employee
            {
                EmpID = request.EmpID,
                EmpName = request.EmpName,
                emailid = request.Email,
                password = request.Password, // Will be hashed by repository
                mobile = request.Mobile,
                Department = request.Department,
                Designation = request.Designation
            };

            await _employeeRepository.CreateEmployeeAsync(employee);

            BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(
                request.Email,
                "Welcome to the Library",
                $"Hello {request.EmpName},<br><br>Welcome to the Library System!<br>Your login is this email address, and your initial password is: <b>{request.Password}</b><br><br>Please log in and change your password."
            ));

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var email = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var employee = await _employeeRepository.GetEmployeeByEmailAsync(email);
            if (employee == null) return NotFound("User not found");

            bool isOldPasswordValid = false;
            if (!string.IsNullOrEmpty(employee.password))
            {
                if (employee.password.StartsWith("$2"))
                {
                    isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, employee.password);
                }
                else
                {
                    isOldPasswordValid = (employee.password == request.OldPassword);
                }
            }

            if (!isOldPasswordValid)
            {
                return BadRequest(new { message = "Incorrect old password." });
            }

            employee.password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _employeeRepository.UpdateEmployeeAsync(employee);

            // Revoke all refresh tokens on password change
            await _refreshTokenRepository.RevokeAllTokensForUserAsync(employee.EmpID);

            BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(
                employee.emailid,
                "Password Changed Successfully",
                $"Hello {employee.EmpName},<br><br>Your library account password has been successfully changed.<br>If you did not perform this action, please contact the IT department immediately."
            ));

            return Ok(new { message = "Password updated successfully." });
        }

        [HttpPost("force-change-password")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginRateLimiter")]
        public async Task<IActionResult> ForceChangePassword([FromBody] ForceChangePasswordRequest request)
        {
            var employee = await _employeeRepository.GetEmployeeByEmailAsync(request.Email);
            if (employee == null) return NotFound(new { message = "User not found" });

            if (string.IsNullOrEmpty(employee.password) || !employee.password.StartsWith("$2"))
            {
                return BadRequest(new { message = "Invalid user state." });
            }

            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, employee.password);

            if (!isOldPasswordValid || request.OldPassword != "Library@123")
            {
                return BadRequest(new { message = "Invalid old password." });
            }

            employee.password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _employeeRepository.UpdateEmployeeAsync(employee);

            await _auditLogRepository.LogActionAsync("ForceChangePassword", employee.EmpID, "Employee", employee.EmpID, "Employee forced to change default password on first login.");

            return Ok(new { message = "Password updated successfully. Please log in with your new password." });
        }

        [HttpPost("admin-change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChangePassword([FromBody] AdminChangePasswordRequest request)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(request.EmpId);
            if (employee == null) return NotFound(new { message = "User not found" });

            employee.password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _employeeRepository.UpdateEmployeeAsync(employee);

            // Revoke all refresh tokens on admin reset
            await _refreshTokenRepository.RevokeAllTokensForUserAsync(employee.EmpID);

            BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(
                employee.emailid,
                "Password Reset by Administrator",
                $"Hello {employee.EmpName},<br><br>Your library account password has been reset by an administrator.<br>Your new password is: <b>{request.NewPassword}</b><br><br>Please log in and change your password if needed."
            ));

            return Ok(new { message = "Employee password updated successfully." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenResponse request)
        {
            if (request == null || string.IsNullOrEmpty(request.RefreshToken) || string.IsNullOrEmpty(request.AccessToken))
            {
                return BadRequest(new { message = "Invalid client request" });
            }

            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                return BadRequest(new { message = "Invalid access token" });
            }

            var email = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Invalid token claims" });
            }

            var employee = await _employeeRepository.GetEmployeeByEmailAsync(email);
            if (employee == null)
            {
                return BadRequest(new { message = "User not found" });
            }

            var cachedRefreshToken = await _refreshTokenRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (cachedRefreshToken == null || cachedRefreshToken.EmpID != employee.EmpID)
            {
                return BadRequest(new { message = "Invalid refresh token" });
            }

            // Detect refresh token reuse! If token is already revoked, revoke all tokens for this user!
            if (cachedRefreshToken.IsRevoked)
            {
                await _refreshTokenRepository.RevokeAllTokensForUserAsync(employee.EmpID);
                await _auditLogRepository.LogActionAsync("RefreshTokenReuseDetected", employee.EmpID, "Employee", employee.EmpID, "Revoked all refresh tokens due to reuse attempt.");
                return Unauthorized(new { message = "Session compromised. Please log in again." });
            }

            if (cachedRefreshToken.IsExpired)
            {
                return Unauthorized(new { message = "Expired session. Please log in again." });
            }

            // Generate new token pair
            var newAccessToken = GenerateJwtTokenInternal(employee, out var newRefreshTokenStr);
            var newRefreshToken = new UserRefreshToken
            {
                EmpID = employee.EmpID,
                Token = newRefreshTokenStr,
                Expiry = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            // Rotate the refresh token: revoke current and link to new
            cachedRefreshToken.IsRevoked = true;
            cachedRefreshToken.RevokedAt = DateTime.UtcNow;
            cachedRefreshToken.ReplacedByToken = newRefreshTokenStr;

            await _refreshTokenRepository.UpdateRefreshTokenAsync(cachedRefreshToken);
            await _refreshTokenRepository.SaveRefreshTokenAsync(newRefreshToken);

            await _auditLogRepository.LogActionAsync("TokenRefreshed", employee.EmpID, "Employee", employee.EmpID, "Successfully refreshed token session.");

            return Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenStr
            });
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured.");
            }

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = false, // We check expired access tokens specifically here
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private async Task<TokenResponse> GenerateJwtTokenAsync(Employee employee)
        {
            var accessTokenStr = GenerateJwtTokenInternal(employee, out var refreshTokenStr);

            var userRefreshToken = new UserRefreshToken
            {
                EmpID = employee.EmpID,
                Token = refreshTokenStr,
                Expiry = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokenRepository.SaveRefreshTokenAsync(userRefreshToken);

            return new TokenResponse
            {
                AccessToken = accessTokenStr,
                RefreshToken = refreshTokenStr
            };
        }

        private string GenerateJwtTokenInternal(Employee employee, out string refreshToken)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey) || jwtKey == "A_very_long_super_secret_key_that_needs_to_be_at_least_32_bytes")
            {
                throw new InvalidOperationException("JWT Secret Key is not properly configured in application configuration.");
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, employee.emailid),
                new Claim("empId", employee.EmpID),
                new Claim("empName", employee.EmpName),
                new Claim(ClaimTypes.Role, employee.IsAdmin ? "Admin" : "User") 
            };

            var accessToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: credentials);

            refreshToken = Guid.NewGuid().ToString();
            return new JwtSecurityTokenHandler().WriteToken(accessToken);
        }
    }
}
