using System.Text;
using Library.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;
    
    public ReportsController(IReportService service)
    {
        _service = service;
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooksReport()
    {
        var csv = await _service.GetBooksReportCsvAsync();
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"books_report_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("active-issues")]
    public async Task<IActionResult> GetActiveIssuesReport()
    {
        var csv = await _service.GetActiveIssuesReportCsvAsync();
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"active_issues_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("issue-history")]
    public async Task<IActionResult> GetIssueHistoryReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var csv = await _service.GetIssueHistoryReportCsvAsync(startDate, endDate);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"issue_history_{DateTime.Now:yyyyMMdd}.csv");
    }
}
