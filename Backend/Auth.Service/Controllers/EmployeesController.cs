using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Domain;
using Shared.Core.DTOs;
using Shared.Core.Interfaces;
using Shared.Infrastructure.Repositories;

namespace Auth.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public EmployeesController(IEmployeeRepository employeeRepository, IAuditLogRepository auditLogRepository)
        {
            _employeeRepository = employeeRepository;
            _auditLogRepository = auditLogRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _employeeRepository.GetAllEmployeesAsync();
            return Ok(employees);
        }

        [HttpGet("{empId}")]
        public async Task<IActionResult> GetById(string empId)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
            if (employee == null) return NotFound(new { Message = "Employee not found." });
            return Ok(employee);
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] Employee employee)
        {
            if (string.IsNullOrEmpty(employee.EmpID) || string.IsNullOrEmpty(employee.EmpName) || string.IsNullOrEmpty(employee.emailid))
            {
                return BadRequest(new { Message = "EmpID, Name, and Email are required." });
            }

            var existingUser = await _employeeRepository.GetEmployeeByIdAsync(employee.EmpID);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "An employee with this ID already exists." });
            }

            if (string.IsNullOrEmpty(employee.password))
            {
                employee.password = "Library@123";
            }

            if (string.IsNullOrEmpty(employee.EmpType))
            {
                employee.EmpType = "contractor";
            }

            await _employeeRepository.CreateEmployeeAsync(employee);
            return Ok(new { Message = "Employee created successfully." });
        }

        [HttpPut("bulk")]
        public async Task<IActionResult> BulkUpdate([FromBody] List<Employee> employees)
        {
            int updatedCount = 0;
            foreach (var emp in employees)
            {
                var result = await _employeeRepository.UpdateEmployeeAsync(emp);
                if (result > 0) updatedCount++;
            }
            return Ok(new { Message = $"Successfully updated {updatedCount} employees." });
        }

        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Please upload a valid CSV file." });

            int addedCount = 0;
            using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
            {
                // Read header
                var header = await reader.ReadLineAsync();
                
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',');
                    if (values.Length < 3) continue; // Basic validation

                    var employee = new Employee
                    {
                        EmpID = values.Length > 0 ? values[0].Trim().Trim('"') : string.Empty,
                        EmpName = values.Length > 1 ? values[1].Trim().Trim('"') : string.Empty,
                        password = values.Length > 2 && !string.IsNullOrWhiteSpace(values[2].Trim('"')) ? values[2].Trim().Trim('"') : "Library@123",
                        emailid = values.Length > 3 ? values[3].Trim().Trim('"') : string.Empty,
                        mobile = values.Length > 4 ? values[4].Trim().Trim('"') : string.Empty,
                        Department = values.Length > 5 ? values[5].Trim().Trim('"') : string.Empty,
                        Designation = values.Length > 6 ? values[6].Trim().Trim('"') : string.Empty
                    };

                    try
                    {
                        var existingUser = await _employeeRepository.GetEmployeeByEmailAsync(employee.emailid);
                        if (existingUser == null)
                        {
                            await _employeeRepository.CreateEmployeeAsync(employee);
                            addedCount++;
                        }
                    }
                    catch
                    {
                        // Ignore individual row errors to allow others to succeed
                    }
                }
            }

            return Ok(new { Message = $"Successfully uploaded {addedCount} new employees." });
        }

        [HttpPut("{empId}/toggle-mfa")]
        public async Task<IActionResult> ToggleMfa(string empId, [FromQuery] bool isMfaEnabled)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
            if (employee == null) return NotFound(new { Message = "Employee not found." });

            employee.IsMfaEnabled = isMfaEnabled;
            await _employeeRepository.UpdateEmployeeAsync(employee);

            var adminEmpId = User.FindFirst("empId")?.Value ?? "Admin";
            await _auditLogRepository.LogActionAsync("ToggleMFA", adminEmpId, "Employee", employee.EmpID, $"MFA toggled to {isMfaEnabled} for user {employee.emailid}.");

            return Ok(new { Message = $"MFA {(isMfaEnabled ? "enabled" : "disabled")} successfully for employee {empId}." });
        }

        [HttpPut("{empId}/toggle-admin")]
        public async Task<IActionResult> ToggleAdmin(string empId, [FromQuery] bool isAdmin)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
            if (employee == null) return NotFound(new { Message = "Employee not found." });

            employee.IsAdmin = isAdmin;
            await _employeeRepository.UpdateEmployeeAsync(employee);

            var adminEmpId = User.FindFirst("empId")?.Value ?? "Admin";
            await _auditLogRepository.LogActionAsync("ToggleAdmin", adminEmpId, "Employee", employee.EmpID, $"Admin role toggled to {isAdmin} for user {employee.emailid}.");

            return Ok(new { Message = $"Admin role {(isAdmin ? "granted" : "revoked")} successfully for employee {empId}." });
        }

        [HttpPut("bulk-toggle-mfa")]
        public async Task<IActionResult> BulkToggleMfa([FromBody] BulkToggleMfaRequest request)
        {
            if (request == null || request.EmpIds == null) return BadRequest("Invalid request");
            
            var adminEmpId = User.FindFirst("empId")?.Value ?? "Admin";
            int updatedCount = 0;
            foreach (var empId in request.EmpIds)
            {
                var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
                if (employee != null)
                {
                    employee.IsMfaEnabled = request.IsMfaEnabled;
                    var res = await _employeeRepository.UpdateEmployeeAsync(employee);
                    if (res > 0)
                    {
                        updatedCount++;
                        await _auditLogRepository.LogActionAsync("ToggleMFA", adminEmpId, "Employee", employee.EmpID, $"MFA bulk toggled to {request.IsMfaEnabled} for user {employee.emailid}.");
                    }
                }
            }
            return Ok(new { Message = $"Successfully updated MFA status for {updatedCount} employees." });
        }
    }
}
