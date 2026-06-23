using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Shared.Core.Domain;
using Shared.Core.Interfaces;

namespace Shared.Infrastructure.Services
{
    public class DarwinboxService : IDarwinboxService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DarwinboxService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var baseUrl = _configuration["Darwinbox:BaseUrl"] ?? "https://jslhrms.darwinbox.in/masterapi/employee";
            var apiKey = _configuration["Darwinbox:EmployeeApiKey"];
            var datasetKey = _configuration["Darwinbox:EmployeeDatasetKey"];
            var userId = _configuration["Darwinbox:UserId"];
            var password = _configuration["Darwinbox:Password"];

            var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userId}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var payload = new
            {
                api_key = apiKey,
                datasetKey = datasetKey
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            string responseString;
            try
            {
                var response = await _httpClient.PostAsync(baseUrl, content);
                response.EnsureSuccessStatusCode();
                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                // Fallback to local JSON file if network sync fails
                var localFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "darwinbox_response.json");
                if (System.IO.File.Exists(localFilePath))
                {
                    responseString = await System.IO.File.ReadAllTextAsync(localFilePath);
                }
                else
                {
                    throw new Exception($"Failed to sync with Darwinbox API ({ex.Message}) and local backup file 'darwinbox_response.json' was not found at {localFilePath}.", ex);
                }
            }

            var result = JsonSerializer.Deserialize<DarwinboxEmployeeResponse>(responseString);

            var employees = new List<Employee>();

            if (result != null && result.EmployeeData != null)
            {
                foreach (var data in result.EmployeeData)
                {
                    employees.Add(new Employee
                    {
                        EmpID = data.EmployeeId,
                        EmpName = $"{data.FirstName} {data.LastName}".Trim(),
                        emailid = data.CompanyEmailId,
                        mobile = data.PrimaryMobileNumber,
                        Department = data.DepartmentName,
                        Designation = data.DesignationName,
                        // Default values
                        password = ""
                    });
                }
            }

            return employees;
        }

        public async Task<string> GetProfilePicAsync(string employeeNo)
        {
            try
            {
                var baseUrl = _configuration["Darwinbox:BaseUrl"] ?? "https://jslhrms.darwinbox.in/masterapi/employee";
                var apiKey = _configuration["Darwinbox:ProfilePicApiKey"];
                var userId = _configuration["Darwinbox:UserId"];
                var password = _configuration["Darwinbox:Password"];

                var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userId}:{password}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

                var payload = new
                {
                    api_key = apiKey,
                    employee_no = employeeNo,
                    @for = "profile_pic"
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(baseUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
            catch (Exception ex)
            {
                // Return a default JSON indicating profile picture is unavailable offline
                return JsonSerializer.Serialize(new { status = 0, message = $"Offline: Profile picture unavailable ({ex.Message})" });
            }
        }
    }
}
