using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using Shared.Core.Domain;
using Shared.Infrastructure.Repositories;

namespace Catalog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;

        public BooksController(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _bookRepository.GetAllBooksAsync();
            return Ok(books);
        }

        [HttpGet("{anum}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookByAnum(long anum)
        {
            var book = await _bookRepository.GetBookByIdAsync(anum);
            if (book == null) return NotFound();
            return Ok(book);
        }

        [HttpPost]
        public async Task<IActionResult> AddBook([FromBody] Book book)
        {
            var result = await _bookRepository.AddBookAsync(book);
            return CreatedAtAction(nameof(GetBookByAnum), new { anum = book.Anum }, book);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchBooks([FromQuery] string category = "", [FromQuery] string keyword = "")
        {
            var books = await _bookRepository.SearchBooksAsync(category ?? "", keyword ?? "");
            return Ok(books);
        }

        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _bookRepository.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost("upload-cover")]
        public async Task<IActionResult> UploadCover(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided");
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = System.Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return Ok(new { CoverImagePath = $"/images/{uniqueFileName}" });
        }

        [HttpPost("bulk-upload")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Please upload a valid CSV file." });

            int addedCount = 0;
            using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
            {
                var header = await reader.ReadLineAsync(); // Read header
                
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',');
                    if (values.Length < 2) continue;

                    var book = new Book
                    {
                        Anum = values.Length > 0 && long.TryParse(values[0].Trim(), out var anum) ? anum : 0,
                        Book_name = values.Length > 1 ? values[1].Trim() : string.Empty,
                        Book_author = values.Length > 2 ? values[2].Trim() : string.Empty,
                        Book_rack = values.Length > 3 ? values[3].Trim() : string.Empty,
                        Book_class = values.Length > 4 ? values[4].Trim() : string.Empty,
                        Book_category = values.Length > 5 ? values[5].Trim() : string.Empty,
                        Publisher = values.Length > 6 ? values[6].Trim() : string.Empty,
                        ISBN = values.Length > 7 ? values[7].Trim() : string.Empty,
                        Edition = values.Length > 8 ? values[8].Trim() : string.Empty,
                        TotalCopies = values.Length > 9 && int.TryParse(values[9].Trim(), out var copies) ? copies : 1,
                        CoverImagePath = values.Length > 10 ? values[10].Trim() : string.Empty
                    };
                    book.Available = book.TotalCopies > 0;

                    if (book.Anum == 0 || string.IsNullOrEmpty(book.Book_name)) continue; // Skip invalid rows

                    try
                    {
                        await _bookRepository.AddBookAsync(book);
                        addedCount++;
                    }
                    catch
                    {
                        // Ignore individual row errors
                    }
                }
            }

            return Ok(new { Message = $"Successfully uploaded {addedCount} new books." });
        }
    }
}
