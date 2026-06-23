using Library.API.DTOs;
using Library.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;
    public EmployeesController(IEmployeeService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllEmployeesAsync());

    [HttpGet("{empId}")]
    public async Task<IActionResult> Get(string empId)
    {
        var emp = await _service.GetEmployeeByIdAsync(empId);
        return emp == null ? NotFound() : Ok(emp);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] EmployeeDto emp) => Ok(await _service.AddEmployeeAsync(emp));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] EmployeeDto emp) => Ok(await _service.UpdateEmployeeAsync(emp));

    [HttpDelete("{empId}")]
    public async Task<IActionResult> Delete(string empId) => Ok(await _service.DeleteEmployeeAsync(empId));
}
