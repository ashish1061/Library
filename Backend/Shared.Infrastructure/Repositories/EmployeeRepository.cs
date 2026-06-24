using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shared.Core.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Shared.Infrastructure.Repositories
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetEmployeeByEmailAsync(string email);
        Task<Employee?> GetEmployeeByIdAsync(string empId);
        Task<int> CreateEmployeeAsync(Employee employee);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<int> UpdateEmployeeAsync(Employee employee);
        Task<int> UpsertEmployeesAsync(List<Employee> employees);
    }

    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? throw new ArgumentNullException("Connection string not found!");
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            using var db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Email", email);

            return await db.QueryFirstOrDefaultAsync<Employee>(
                "SELECT * FROM Employee WHERE emailid = @Email AND (IsDeleted = 0 OR IsDeleted IS NULL) ORDER BY EmpID",
                parameters);
        }

        public async Task<int> CreateEmployeeAsync(Employee employee)
        {
            if (!string.IsNullOrEmpty(employee.password) && !employee.password.StartsWith("$2"))
            {
                employee.password = BCrypt.Net.BCrypt.HashPassword(employee.password);
            }

            using var db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@EmpID", employee.EmpID);
            parameters.Add("@EmpName", employee.EmpName);
            parameters.Add("@Email", employee.emailid);
            parameters.Add("@Password", employee.password);
            parameters.Add("@Mobile", employee.mobile);
            parameters.Add("@Department", employee.Department);
            parameters.Add("@Designation", employee.Designation);
            parameters.Add("@ImagePath", employee.ImagePath);
            parameters.Add("@IsMfaEnabled", employee.IsMfaEnabled);
            parameters.Add("@FailedLoginAttempts", employee.FailedLoginAttempts);
            parameters.Add("@LockoutEnd", employee.LockoutEnd);
            parameters.Add("@IsAdmin", employee.IsAdmin);

            var query = @"
                INSERT INTO Employee (EmpID, EmpName, emailid, password, mobile, Department, Designation, ImagePath, IsMfaEnabled, FailedLoginAttempts, LockoutEnd, IsAdmin)
                VALUES (@EmpID, @EmpName, @Email, @Password, @Mobile, @Department, @Designation, @ImagePath, @IsMfaEnabled, @FailedLoginAttempts, @LockoutEnd, @IsAdmin);
            ";

            return await db.ExecuteAsync(query, parameters);
        }

        public async Task<Employee?> GetEmployeeByIdAsync(string empId)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<Employee>(
                "SELECT * FROM Employee WHERE EmpID = @EmpID AND (IsDeleted = 0 OR IsDeleted IS NULL)",
                new { EmpID = empId });
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<Employee>("SELECT * FROM Employee WHERE IsDeleted = 0 OR IsDeleted IS NULL");
        }

        public async Task<int> UpdateEmployeeAsync(Employee employee)
        {
            if (!string.IsNullOrEmpty(employee.password) && !employee.password.StartsWith("$2"))
            {
                employee.password = BCrypt.Net.BCrypt.HashPassword(employee.password);
            }

            using var db = new SqlConnection(_connectionString);
            var query = @"
                UPDATE Employee 
                SET EmpName = @EmpName, 
                    emailid = @Email, 
                    password = @Password,
                    mobile = @Mobile,
                    Department = @Department,
                    Designation = @Designation,
                    ImagePath = @ImagePath,
                    IsMfaEnabled = @IsMfaEnabled,
                    FailedLoginAttempts = @FailedLoginAttempts,
                    LockoutEnd = @LockoutEnd,
                    IsAdmin = @IsAdmin
                WHERE EmpID = @EmpID
            ";
            
            var parameters = new DynamicParameters();
            parameters.Add("@EmpID", employee.EmpID);
            parameters.Add("@EmpName", employee.EmpName);
            parameters.Add("@Email", employee.emailid);
            parameters.Add("@Password", employee.password);
            parameters.Add("@Mobile", employee.mobile);
            parameters.Add("@Department", employee.Department);
            parameters.Add("@Designation", employee.Designation);
            parameters.Add("@ImagePath", employee.ImagePath);
            parameters.Add("@IsMfaEnabled", employee.IsMfaEnabled);
            parameters.Add("@FailedLoginAttempts", employee.FailedLoginAttempts);
            parameters.Add("@LockoutEnd", employee.LockoutEnd);
            parameters.Add("@IsAdmin", employee.IsAdmin);

            return await db.ExecuteAsync(query, parameters);
        }

        public async Task<int> UpsertEmployeesAsync(List<Employee> employees)
        {
            using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();

            // Fetch existing employee IDs to avoid hashing password using BCrypt for existing users
            var existingIds = (await db.QueryAsync<string>("SELECT EmpID FROM Employee")).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var employee in employees)
            {
                if (!existingIds.Contains(employee.EmpID))
                {
                    if (string.IsNullOrEmpty(employee.password) || !employee.password.StartsWith("$2"))
                    {
                        employee.password = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrEmpty(employee.password) ? "Library@123" : employee.password);
                    }
                }
                else
                {
                    // Match exists, SQL MERGE won't update password so set to empty to bypass BCrypt hashing
                    employee.password = "";
                }
            }

            using var transaction = db.BeginTransaction();

            int rowsAffected = 0;
            try
            {
                var query = @"
                    MERGE INTO Employee AS target
                    USING (SELECT @EmpID as EmpID) AS source
                    ON target.EmpID = source.EmpID
                    WHEN MATCHED THEN 
                        UPDATE SET 
                            EmpName = @EmpName,
                            emailid = @emailid,
                            mobile = @mobile,
                            Department = @Department,
                            Designation = @Designation,
                            IsDeleted = 0,
                            IsAdmin = @IsAdmin
                    WHEN NOT MATCHED THEN
                        INSERT (EmpID, EmpName, password, emailid, mobile, Department, Designation, EmpType, IsMfaEnabled, IsAdmin)
                        VALUES (@EmpID, @EmpName, @password, @emailid, @mobile, @Department, @Designation, 'darwinbox', 1, @IsAdmin);
                ";

                rowsAffected = await db.ExecuteAsync(query, employees, transaction: transaction);

                if (employees.Any())
                {
                    var empIds = employees.Select(e => e.EmpID).ToList();
                    var joinedEmpIds = string.Join(",", empIds);
                    
                    var deleteQuery = @"
                        UPDATE Employee 
                        SET IsDeleted = 1 
                        WHERE EmpType = 'darwinbox' AND EmpID NOT IN (SELECT value FROM STRING_SPLIT(@JoinedEmpIds, ','))";
                        
                    await db.ExecuteAsync(deleteQuery, new { JoinedEmpIds = joinedEmpIds }, transaction: transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return rowsAffected;
        }
    }
}
