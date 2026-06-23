using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Domain;
using Shared.Infrastructure.Repositories;
using Shared.Core.Interfaces;

namespace Operations.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IssuesController : ControllerBase
    {
        private readonly IIssueRepository _issueRepository;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateRepository _templateRepository;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public IssuesController(IIssueRepository issueRepository, IEmailService emailService, IEmailTemplateRepository templateRepository, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _issueRepository = issueRepository;
            _emailService = emailService;
            _templateRepository = templateRepository;
            _configuration = configuration;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveIssues()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var empId = User.FindFirst("empId")?.Value;

            var issues = await _issueRepository.GetActiveIssuesAsync();
            if (userRole != "Admin")
            {
                issues = issues.Where(x => x.EmpID == empId).ToList();
            }

            await EnrichIssuesWithImages(issues);
            return Ok(issues);
        }

        [HttpGet("book/{anum}")]
        public async Task<IActionResult> GetActiveIssuesByAnum(long anum)
        {
            var issues = await _issueRepository.GetActiveIssuesByAnumAsync(anum);
            await EnrichIssuesWithImages(issues);
            return Ok(issues);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetIssueHistory()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var empId = User.FindFirst("empId")?.Value;

            var history = await _issueRepository.GetIssueHistoryAsync();
            if (userRole != "Admin")
            {
                history = history.Where(x => x.EmpID == empId).ToList();
            }

            await EnrichIssuesWithImages(history);
            return Ok(history);
        }

        private async Task EnrichIssuesWithImages<T>(IEnumerable<T> issues) where T : class
        {
            if (issues == null || !issues.Any()) return;

            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            var handler = isDevelopment 
                ? new System.Net.Http.HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true } 
                : null;

            using var secureClient = handler != null ? new System.Net.Http.HttpClient(handler) : new System.Net.Http.HttpClient();

            if (!string.IsNullOrEmpty(token))
            {
                secureClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var catalogUrl = _configuration["ServiceUrls:CatalogService"] ?? "https://localhost:7002";
            var authUrl = _configuration["ServiceUrls:AuthService"] ?? "https://localhost:7001";

            // Fetch books from Catalog.Service
            List<Book>? books = null;
            try
            {
                var booksResponse = await secureClient.GetAsync($"{catalogUrl}/api/books");
                if (booksResponse.IsSuccessStatusCode)
                    books = await booksResponse.Content.ReadFromJsonAsync<List<Book>>();
            } catch { }

            // Fetch employees from Auth.Service
            List<Employee>? employees = null;
            try 
            {
                var empResponse = await secureClient.GetAsync($"{authUrl}/api/employees");
                if (empResponse.IsSuccessStatusCode)
                    employees = await empResponse.Content.ReadFromJsonAsync<List<Employee>>();
            } catch { }

            // Fetch magazines from Catalog.Service
            List<Magazine>? magazines = null;
            try
            {
                var magResponse = await secureClient.GetAsync($"{catalogUrl}/api/magazines");
                if (magResponse.IsSuccessStatusCode)
                    magazines = await magResponse.Content.ReadFromJsonAsync<List<Magazine>>();
            } catch { }

            foreach (var issue in issues)
            {
                if (issue is Issue i)
                {
                    if (i.ItemType == "Magazine") {
                        var m = magazines?.FirstOrDefault(x => x.MagazineId == i.Anum);
                        if (m != null) i.CoverImagePath = m.CoverImagePath;
                    } else {
                        var b = books?.FirstOrDefault(x => x.Anum == i.Anum);
                        if (b != null) i.CoverImagePath = b.CoverImagePath;
                    }
                    var e = employees?.FirstOrDefault(x => x.EmpID == i.EmpID);
                    if (e != null) i.EmployeeImagePath = e.ImagePath ?? string.Empty;
                }
                else if (issue is IssueHistory h)
                {
                    if (h.ItemType == "Magazine") {
                        var m = magazines?.FirstOrDefault(x => x.MagazineId == h.Anum);
                        if (m != null) h.CoverImagePath = m.CoverImagePath;
                    } else {
                        var b = books?.FirstOrDefault(x => x.Anum == h.Anum);
                        if (b != null) h.CoverImagePath = b.CoverImagePath;
                    }
                    var e = employees?.FirstOrDefault(x => x.EmpID == h.EmpID);
                    if (e != null) h.EmployeeImagePath = e.ImagePath ?? string.Empty;
                }
                else if (issue is IssueRequest r)
                {
                    if (r.ItemType == "Magazine") {
                        var m = magazines?.FirstOrDefault(x => x.MagazineId == r.ItemID);
                        if (m != null) r.CoverImagePath = m.CoverImagePath;
                    } else {
                        var b = books?.FirstOrDefault(x => x.Anum == r.ItemID);
                        if (b != null) r.CoverImagePath = b.CoverImagePath;
                    }
                    var e = employees?.FirstOrDefault(x => x.EmpID == r.EmpID);
                    if (e != null) r.EmployeeImagePath = e.ImagePath ?? string.Empty;
                }
            }
        }

        [HttpPost("issue")]
        public async Task<IActionResult> IssueBook([FromBody] Issue issue)
        {
            var email = issue.EmpID;
            var employee = await _issueRepository.GetEmployeeByEmailAsync(email);
            if (employee == null)
            {
                return BadRequest(new { Message = $"Cannot issue item: Employee with email '{email}' not found." });
            }

            // Map actual employee ID and name
            issue.EmpID = employee.EmpID;
            issue.EmpName = employee.EmpName;

            var result = await _issueRepository.IssueBookAsync(issue);
            
            // Send Email confirmation asynchronously using Hangfire
            if (result > 0)
            {
                var template = await _templateRepository.GetTemplateByPurposeAsync("Issue Confirmation");
                var recipientEmail = employee.emailid;
                var subject = template?.Subject ?? "";
                var body = template?.Body ?? "";

                body = body.Replace("{{EmployeeName}}", issue.EmpName)
                           .Replace("{{BookName}}", issue.BookName)
                           .Replace("{{DueDate}}", issue.DueDate?.ToString("dd-MM-yyyy") ?? "");
                subject = subject.Replace("{{EmployeeName}}", issue.EmpName).Replace("{{BookName}}", issue.BookName);

                Hangfire.BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(
                    recipientEmail,
                    subject,
                    body
                ));
            }
            
            return Ok(new { Message = "Book issued successfully", IssueNumber = result });
        }

        [HttpPost("return")]
        public async Task<IActionResult> ReturnBook([FromBody] ReturnRequest request)
        {
            var result = await _issueRepository.ReturnBookAsync(request.IssueNumber, request.ReturnDate);
            if (result > 0)
                return Ok(new { Message = "Book returned successfully" });
            return BadRequest(new { Message = "Failed to return book" });
        }

        [HttpPost("reissue/{issueNumber}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReissueBook(int issueNumber)
        {
            var result = await _issueRepository.ReissueBookAsync(issueNumber);
            if (result > 0)
                return Ok(new { Message = "Book reissued successfully" });
            return BadRequest(new { Message = "Failed to reissue book" });
        }

        [HttpPost("remind")]
        public async Task<IActionResult> SendReminders([FromBody] List<int> issueNumbers)
        {
            if (issueNumbers == null || !issueNumbers.Any())
                return BadRequest(new { Message = "No issues selected" });

            var activeIssues = await _issueRepository.GetActiveIssuesAsync();
            var selectedIssues = activeIssues.Where(i => issueNumbers.Contains(i.IssueNumber)).ToList();

            var overdueTemplate = await _templateRepository.GetTemplateByPurposeAsync("Overdue Reminder");
            var upcomingTemplate = await _templateRepository.GetTemplateByPurposeAsync("Upcoming Due Date");

            int sentCount = 0;
            foreach (var issue in selectedIssues)
            {
                // Select template based on whether the due date is in the future or past
                var isFutureDate = issue.DueDate.HasValue && issue.DueDate.Value.Date >= DateTime.Today;
                var template = isFutureDate ? upcomingTemplate : overdueTemplate;

                var dbEmail = await _issueRepository.GetEmployeeEmailAsync(issue.EmpID);
                var recipientEmail = !string.IsNullOrEmpty(dbEmail) ? dbEmail : $"{issue.EmpID}@corp.com";
                var subject = template?.Subject ?? "";
                var body = template?.Body ?? "";

                body = body.Replace("{{EmployeeName}}", issue.EmpName)
                           .Replace("{{BookName}}", issue.BookName)
                           .Replace("{{DueDate}}", issue.DueDate?.ToString("dd-MM-yyyy") ?? "");
                           
                if (isFutureDate)
                {
                    int daysRemaining = (issue.DueDate!.Value.Date - DateTime.Today).Days;
                    string dayString = daysRemaining == 1 ? "1 day" : $"{daysRemaining} days";
                    body = body.Replace("{{DaysRemaining}} days", dayString)
                               .Replace("{{DaysRemaining}}", daysRemaining.ToString());
                }

                subject = subject.Replace("{{EmployeeName}}", issue.EmpName).Replace("{{BookName}}", issue.BookName);

                // Log the email into the database
                await _issueRepository.LogEmailAsync(issue.EmpID, recipientEmail, subject, body, issue.IssueNumber);

                // Send email using Hangfire
                Hangfire.BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(
                    recipientEmail,
                    subject,
                    body
                ));
                sentCount++;
            }

            return Ok(new { Message = $"{sentCount} reminders sent successfully" });
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportIssues([FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            var history = await _issueRepository.GetIssueHistoryAsync();
            var filtered = history.ToList();

            if (DateTime.TryParse(startDate, out var start) && DateTime.TryParse(endDate, out var end))
            {
                filtered = filtered.Where(x => {
                    if (DateTime.TryParse(x.IssueDate, out var issueDate))
                        return issueDate >= start && issueDate <= end;
                    return false;
                }).ToList();
            }

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Issue History");
            var currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Issue Number";
            worksheet.Cell(currentRow, 2).Value = "Book Name";
            worksheet.Cell(currentRow, 3).Value = "Emp Name";
            worksheet.Cell(currentRow, 4).Value = "Issue Date";
            worksheet.Cell(currentRow, 5).Value = "Return Date";

            foreach (var item in filtered)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = item.IssueNumber;
                worksheet.Cell(currentRow, 2).Value = item.BookName;
                worksheet.Cell(currentRow, 3).Value = item.EmpName;
                worksheet.Cell(currentRow, 4).Value = item.IssueDate;
                worksheet.Cell(currentRow, 5).Value = item.ReturnDate;
            }

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "IssueHistory.xlsx");
        }
        // --- ISSUE REQUESTS ---

        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest([FromBody] IssueRequestDto dto)
        {
            var req = new IssueRequest
            {
                EmpID = dto.EmpID,
                ItemType = dto.ItemType,
                ItemID = dto.ItemID,
                ItemName = dto.ItemName,
                RequestDate = DateTime.Now,
                Status = "Pending"
            };
            var result = await _issueRepository.CreateRequestAsync(req);
            
            // Send email notification about request
            var template = await _templateRepository.GetTemplateByPurposeAsync("Issue Request");
            var dbEmail = await _issueRepository.GetEmployeeEmailAsync(dto.EmpID);
            var recipientEmail = !string.IsNullOrEmpty(dbEmail) ? dbEmail : $"{dto.EmpID}@corp.com";
            var subject = template?.Subject ?? "";
            var body = template?.Body ?? "";
            
            body = body.Replace("{{EmployeeName}}", dto.EmpID).Replace("{{BookName}}", dto.ItemName);
            subject = subject.Replace("{{EmployeeName}}", dto.EmpID).Replace("{{BookName}}", dto.ItemName);

            Hangfire.BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(recipientEmail, subject, body));

            return Ok(new { Message = "Request created successfully", RequestID = result });
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetAllRequests()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var empId = User.FindFirst("empId")?.Value;

            var requests = await _issueRepository.GetAllRequestsAsync();
            if (userRole != "Admin")
            {
                requests = requests.Where(x => x.EmpID == empId).ToList();
            }

            await EnrichIssuesWithImages(requests);
            return Ok(requests);
        }

        [HttpGet("requests/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _issueRepository.GetPendingRequestsAsync();
            await EnrichIssuesWithImages(requests);
            return Ok(requests);
        }

        [HttpPost("requests/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRequests([FromBody] List<int> requestIds)
        {
            if (requestIds == null || !requestIds.Any()) return BadRequest("No requests selected");

            int approvedCount = 0;
            foreach (var reqId in requestIds)
            {
                var req = await _issueRepository.GetRequestByIdAsync(reqId);
                if (req != null && req.Status == "Pending")
                {
                    var dbEmail = await _issueRepository.GetEmployeeEmailAsync(req.EmpID);
                    if (string.IsNullOrEmpty(req.EmpID) || string.IsNullOrEmpty(dbEmail))
                    {
                        continue; // Skip approving this request if the employee lacks ID/Email
                    }

                    // Update status
                    await _issueRepository.UpdateRequestStatusAsync(reqId, "Approved");
                    
                    // Automatically issue the book/magazine
                    var issue = new Issue
                    {
                        Anum = req.ItemID,
                        BookName = req.ItemName,
                        EmpID = req.EmpID,
                        EmpName = req.EmpName,
                        IssueDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        ItemType = req.ItemType
                    };
                    await _issueRepository.IssueBookAsync(issue);

                    // Send approval email
                    var template = await _templateRepository.GetTemplateByPurposeAsync("Request Approved");
                    var recipientEmail = !string.IsNullOrEmpty(dbEmail) ? dbEmail : $"{req.EmpID}@corp.com";
                    var subject = template?.Subject ?? "";
                    var body = template?.Body ?? "";
                    
                    body = body.Replace("{{EmployeeName}}", req.EmpName ?? req.EmpID).Replace("{{BookName}}", req.ItemName);
                    subject = subject.Replace("{{EmployeeName}}", req.EmpName ?? req.EmpID).Replace("{{BookName}}", req.ItemName);

                    Hangfire.BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(recipientEmail, subject, body));

                    approvedCount++;
                }
            }
            return Ok(new { Message = $"{approvedCount} requests approved." });
        }

        [HttpPost("requests/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectRequests([FromBody] List<int> requestIds)
        {
            if (requestIds == null || !requestIds.Any()) return BadRequest("No requests selected");

            int rejectedCount = 0;
            foreach (var reqId in requestIds)
            {
                var req = await _issueRepository.GetRequestByIdAsync(reqId);
                if (req != null && req.Status == "Pending")
                {
                    // Update status
                    await _issueRepository.UpdateRequestStatusAsync(reqId, "Rejected");

                    // Send rejection email
                    var template = await _templateRepository.GetTemplateByPurposeAsync("Request Rejected");
                    var dbEmail = await _issueRepository.GetEmployeeEmailAsync(req.EmpID);
                    var recipientEmail = !string.IsNullOrEmpty(dbEmail) ? dbEmail : $"{req.EmpID}@corp.com";
                    var subject = template?.Subject ?? "";
                    var body = template?.Body ?? "";
                    
                    body = body.Replace("{{EmployeeName}}", req.EmpName ?? req.EmpID).Replace("{{BookName}}", req.ItemName);
                    subject = subject.Replace("{{EmployeeName}}", req.EmpName ?? req.EmpID).Replace("{{BookName}}", req.ItemName);

                    Hangfire.BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(recipientEmail, subject, body));

                    rejectedCount++;
                }
            }
            return Ok(new { Message = $"{rejectedCount} requests rejected." });
        }
    }

    public class ReturnRequest
    {
        public int IssueNumber { get; set; }
        public string ReturnDate { get; set; } = string.Empty;
    }
}
