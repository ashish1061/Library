using Library.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _service;
    public AnalyticsController(IAnalyticsService service) => _service = service;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() => Ok(await _service.GetSummaryAsync());

    [HttpGet("books-by-category")]
    public async Task<IActionResult> GetBooksByCategory() => Ok(await _service.GetBooksByCategoryAsync());

    [HttpGet("issue-trends")]
    public async Task<IActionResult> GetIssueTrends() => Ok(await _service.GetIssueTrendsAsync());
}
