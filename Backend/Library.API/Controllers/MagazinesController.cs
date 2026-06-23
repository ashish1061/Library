using Library.API.DTOs;
using Library.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MagazinesController : ControllerBase
{
    private readonly IMagazineService _service;
    public MagazinesController(IMagazineService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllMagazinesAsync());

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] MagazineDto magazine) => Ok(await _service.AddMagazineAsync(magazine));
}
