using Library.API.DTOs;
using Library.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IBookService _service;
    public BooksController(IBookService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllBooksAsync());

    [HttpGet("{anum}")]
    public async Task<IActionResult> Get(long anum)
    {
        var book = await _service.GetBookByAnumAsync(anum);
        return book == null ? NotFound() : Ok(book);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] BookDto book) => Ok(await _service.AddBookAsync(book));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] BookDto book) => Ok(await _service.UpdateBookAsync(book));

    [HttpDelete("{anum}")]
    public async Task<IActionResult> Delete(long anum) => Ok(await _service.DeleteBookAsync(anum));
}
