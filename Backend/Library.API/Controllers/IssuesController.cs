using Library.API.DTOs;
using Library.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _service;
    public IssuesController(IIssueService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllIssuesAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IssueDto issue) => Ok(await _service.CreateIssueAsync(issue));

    [HttpPost("return/{issueNumber}")]
    public async Task<IActionResult> Return(int issueNumber) => Ok(await _service.ReturnIssueAsync(issueNumber));
}
