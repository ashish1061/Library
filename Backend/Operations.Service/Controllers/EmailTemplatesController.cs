using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Interfaces;

namespace Operations.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class EmailTemplatesController : ControllerBase
    {
        private readonly IEmailTemplateRepository _templateRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public EmailTemplatesController(IEmailTemplateRepository templateRepository, IAuditLogRepository auditLogRepository)
        {
            _templateRepository = templateRepository;
            _auditLogRepository = auditLogRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTemplates()
        {
            var templates = await _templateRepository.GetAllTemplatesAsync();
            return Ok(templates);
        }

        [HttpPost]
        public async Task<IActionResult> AddTemplate([FromBody] Shared.Core.Domain.EmailTemplate template)
        {
            if (string.IsNullOrWhiteSpace(template.Purpose) || string.IsNullOrWhiteSpace(template.Subject) || string.IsNullOrWhiteSpace(template.Body))
            {
                return BadRequest(new { Message = "Purpose, Subject, and Body are required." });
            }

            var newId = await _templateRepository.UpsertTemplateAsync(template);
            
            var empId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Admin";
            await _auditLogRepository.LogActionAsync("AddTemplate", empId, "EmailTemplate", newId.ToString(), $"Added template: {template.Purpose}");
            
            return Ok(new { Message = "Template created successfully", TemplateId = newId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] Shared.Core.Domain.EmailTemplate template)
        {
            var existing = await _templateRepository.GetTemplateByIdAsync(id);
            if (existing == null) return NotFound(new { Message = "Template not found" });

            template.TemplateId = id;
            await _templateRepository.UpsertTemplateAsync(template);

            var empId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Admin";
            await _auditLogRepository.LogActionAsync("UpdateTemplate", empId, "EmailTemplate", id.ToString(), $"Updated template: {template.Purpose}");

            return Ok(new { Message = "Template updated successfully" });
        }
    }
}
