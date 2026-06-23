using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shared.Infrastructure.Services
{
    public class DarwinboxEmployeeResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("employee_data")]
        public List<DarwinboxEmployeeData> EmployeeData { get; set; } = new List<DarwinboxEmployeeData>();
    }

    public class DarwinboxEmployeeData
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("last_name")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("designation_name")]
        public string DesignationName { get; set; } = string.Empty;

        [JsonPropertyName("department_name")]
        public string DepartmentName { get; set; } = string.Empty;

        [JsonPropertyName("company_email_id")]
        public string CompanyEmailId { get; set; } = string.Empty;

        [JsonPropertyName("primary_mobile_number")]
        public string PrimaryMobileNumber { get; set; } = string.Empty;

        [JsonPropertyName("group_date_of_joining")]
        public string GroupDateOfJoining { get; set; } = string.Empty;

        [JsonPropertyName("group_company")]
        public string GroupCompany { get; set; } = string.Empty;

        [JsonPropertyName("business_unit")]
        public string BusinessUnit { get; set; } = string.Empty;

        [JsonPropertyName("employee_type")]
        public string EmployeeType { get; set; } = string.Empty;

        [JsonPropertyName("employee_id")]
        public string EmployeeId { get; set; } = string.Empty;
    }
}
