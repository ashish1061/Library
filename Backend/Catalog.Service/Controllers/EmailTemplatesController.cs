using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Domain;
using Shared.Core.Interfaces;

namespace Catalog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmailTemplatesController : ControllerBase
    {
        private readonly IEmailTemplateRepository _repo;

        public EmailTemplatesController(IEmailTemplateRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var templates = await _repo.GetAllTemplatesAsync();
            return Ok(templates);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var template = await _repo.GetTemplateByIdAsync(id);
            if (template == null) return NotFound();
            return Ok(template);
        }

        [HttpGet("purpose/{purpose}")]
        public async Task<IActionResult> GetByPurpose(string purpose)
        {
            var template = await _repo.GetTemplateByPurposeAsync(purpose);
            if (template == null) return NotFound();
            return Ok(template);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] EmailTemplate template)
        {
            if (string.IsNullOrEmpty(template.Purpose) || string.IsNullOrEmpty(template.Subject) || string.IsNullOrEmpty(template.Body))
            {
                return BadRequest("Purpose, Subject, and Body are required.");
            }
            var id = await _repo.UpsertTemplateAsync(template);
            return Ok(new { TemplateId = id, Message = "Template saved successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteTemplateAsync(id);
            return Ok(new { Message = "Template deleted successfully" });
        }
    }
}
