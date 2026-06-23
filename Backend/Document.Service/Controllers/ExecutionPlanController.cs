using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Domain;
using Shared.Infrastructure.Repositories;

namespace Document.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExecutionPlanController : ControllerBase
    {
        private readonly IExecutionPlanRepository _executionPlanRepository;

        public ExecutionPlanController(IExecutionPlanRepository executionPlanRepository)
        {
            _executionPlanRepository = executionPlanRepository;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadExecutionPlan([FromForm] IFormFile file, [FromForm] int userId)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadDirectory)) Directory.CreateDirectory(uploadDirectory);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var plan = new ExecutionPlan
            {
                UserId = userId,
                FileName = file.FileName,
                FilePath = filePath,
                UploadDate = DateTime.Now
            };

            var planId = await _executionPlanRepository.SubmitExecutionPlanAsync(plan);

            return Ok(new { Message = "Execution plan uploaded successfully.", PlanId = planId });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPlans(int userId)
        {
            var plans = await _executionPlanRepository.GetExecutionPlansByUserIdAsync(userId);
            return Ok(plans);
        }
    }
}
