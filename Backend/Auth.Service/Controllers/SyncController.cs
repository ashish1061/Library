using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Core.Interfaces;
using Shared.Infrastructure.Repositories;

namespace Auth.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SyncController : ControllerBase
    {
        private readonly IDarwinboxService _darwinboxService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<SyncController> _logger;

        public SyncController(IDarwinboxService darwinboxService, IEmployeeRepository employeeRepository, ILogger<SyncController> logger)
        {
            _darwinboxService = darwinboxService;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        [HttpPost("darwinbox")]
        public async Task<IActionResult> SyncDarwinbox()
        {
            try
            {
                var employees = await _darwinboxService.GetEmployeesAsync();
                
                if (employees == null || employees.Count == 0)
                {
                    return BadRequest(new { Message = "Failed to fetch employees from Darwinbox or list was empty." });
                }

                var rowsAffected = await _employeeRepository.UpsertEmployeesAsync(employees);

                return Ok(new 
                { 
                    Message = "Successfully synchronized with Darwinbox",
                    EmployeesFetched = employees.Count,
                    RecordsUpdatedOrInserted = rowsAffected
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing with Darwinbox");
                return StatusCode(500, new { Message = "An error occurred while syncing with Darwinbox.", Error = ex.Message });
            }
        }

        [HttpGet("darwinbox/profile-pic/{empId}")]
        public async Task<IActionResult> SyncProfilePic(string empId)
        {
            try
            {
                var profilePicData = await _darwinboxService.GetProfilePicAsync(empId);
                
                // Parse the response from Darwinbox if it's JSON, or just return it
                // We'll return it as JSON payload containing the raw string so the frontend can parse it
                return Ok(new { data = profilePicData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile pic from Darwinbox for {EmpId}", empId);
                return StatusCode(500, new { Message = "An error occurred while fetching profile pic.", Error = ex.Message });
            }
        }
    }
}
