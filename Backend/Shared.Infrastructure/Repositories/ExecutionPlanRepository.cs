using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shared.Core.Domain;

namespace Shared.Infrastructure.Repositories
{
    public interface IExecutionPlanRepository
    {
        Task<int> SubmitExecutionPlanAsync(ExecutionPlan plan);
        Task<IEnumerable<ExecutionPlan>> GetExecutionPlansByUserIdAsync(int userId);
    }

    public class ExecutionPlanRepository : IExecutionPlanRepository
    {
        private readonly string _connectionString;

        public ExecutionPlanRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? string.Empty;
        }

        public async Task<int> SubmitExecutionPlanAsync(ExecutionPlan plan)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", plan.UserId);
            parameters.Add("@FileName", plan.FileName);
            parameters.Add("@FilePath", plan.FilePath);
            parameters.Add("@UploadDate", plan.UploadDate);

            return await db.ExecuteScalarAsync<int>("sp_SubmitExecutionPlan", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<ExecutionPlan>> GetExecutionPlansByUserIdAsync(int userId)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            
            return await db.QueryAsync<ExecutionPlan>("sp_GetExecutionPlansByUserId", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
