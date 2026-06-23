using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Domain;
using Shared.Core.Interfaces;

namespace Catalog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MagazinesController : ControllerBase
    {
        private readonly IMagazineRepository _repo;

        public MagazinesController(IMagazineRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var magazines = await _repo.GetAllMagazinesAsync();
            return Ok(magazines);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var magazine = await _repo.GetMagazineByIdAsync(id);
            if (magazine == null) return NotFound();
            return Ok(magazine);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] Magazine magazine)
        {
            if (magazine.MagazineId <= 0 || string.IsNullOrEmpty(magazine.Title))
                return BadRequest("Invalid magazine data");
                
            await _repo.AddMagazineAsync(magazine);
            return Ok(new { Message = "Magazine saved successfully" });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(long id, [FromBody] Magazine magazine)
        {
            if (id != magazine.MagazineId) return BadRequest("ID mismatch");
            await _repo.UpdateMagazineAsync(magazine);
            return Ok(new { Message = "Magazine updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            await _repo.DeleteMagazineAsync(id);
            return Ok(new { Message = "Magazine deleted successfully" });
        }

        [HttpPost("upload-cover")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadCover(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File not found");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
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
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var header = await reader.ReadLineAsync(); // Read header
                
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',');
                    if (values.Length < 2) continue;

                    var magazine = new Magazine
                    {
                        MagazineId = values.Length > 0 && long.TryParse(values[0].Trim(), out var id) ? id : 0,
                        Title = values.Length > 1 ? values[1].Trim() : string.Empty,
                        Publisher = values.Length > 2 ? values[2].Trim() : string.Empty,
                        IssueDate = values.Length > 3 ? values[3].Trim() : string.Empty,
                        RackLocation = values.Length > 4 ? values[4].Trim() : string.Empty,
                        Category = values.Length > 5 ? values[5].Trim() : string.Empty,
                        TotalCopies = values.Length > 6 && int.TryParse(values[6].Trim(), out var copies) ? copies : 1,
                        CoverImagePath = values.Length > 7 ? values[7].Trim() : string.Empty
                    };
                    magazine.AvailableCopies = magazine.TotalCopies > 0 ? magazine.TotalCopies : 0;

                    if (magazine.MagazineId > 0 && !string.IsNullOrEmpty(magazine.Title))
                    {
                        try
                        {
                            await _repo.AddMagazineAsync(magazine);
                            addedCount++;
                        }
                        catch
                        {
                            // Ignore individual row errors
                        }
                    }
                }
            }

            return Ok(new { Message = $"Successfully uploaded {addedCount} new magazines." });
        }
    }
}
