using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using Shared.Core.Interfaces;

namespace Shared.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly string _connectionString;

        public AuditLogRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? "";
        }

        public async Task<int> LogActionAsync(string action, string empId, string entity, string entityId, string details)
        {
            using var db = new SqlConnection(_connectionString);
            string query = @"
                INSERT INTO AuditLog (Action, EmpID, Entity, EntityId, Timestamp, Details) 
                VALUES (@Action, @EmpID, @Entity, @EntityId, @Timestamp, @Details);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            return await db.QuerySingleOrDefaultAsync<int>(query, new 
            { 
                Action = action, 
                EmpID = empId, 
                Entity = entity, 
                EntityId = entityId, 
                Timestamp = DateTime.Now, 
                Details = details 
            });
        }
    }
}
