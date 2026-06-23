using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using Shared.Core.Domain;
using Shared.Core.Interfaces;

namespace Shared.Infrastructure.Repositories
{
    public class EmailTemplateRepository : IEmailTemplateRepository
    {
        private readonly string _connectionString;

        public EmailTemplateRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? "";
        }

        public async Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync()
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<EmailTemplate>("SELECT * FROM EmailTemplates");
        }

        public async Task<EmailTemplate?> GetTemplateByPurposeAsync(string purpose)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<EmailTemplate>(
                "SELECT * FROM EmailTemplates WHERE Purpose = @Purpose",
                new { Purpose = purpose });
        }

        public async Task<EmailTemplate?> GetTemplateByIdAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<EmailTemplate>(
                "SELECT * FROM EmailTemplates WHERE TemplateId = @TemplateId",
                new { TemplateId = id });
        }

        public async Task<int> UpsertTemplateAsync(EmailTemplate template)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                IF EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateId = @TemplateId OR Purpose = @Purpose)
                BEGIN
                    UPDATE EmailTemplates SET 
                        Subject = @Subject,
                        Body = @Body,
                        Purpose = @Purpose
                    WHERE TemplateId = @TemplateId OR Purpose = @Purpose;
                    SELECT @TemplateId;
                END
                ELSE
                BEGIN
                    INSERT INTO EmailTemplates (Purpose, Subject, Body)
                    VALUES (@Purpose, @Subject, @Body);
                    SELECT CAST(SCOPE_IDENTITY() as int);
                END
            ";
            return await db.ExecuteScalarAsync<int>(query, template);
        }

        public async Task<int> DeleteTemplateAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.ExecuteAsync("DELETE FROM EmailTemplates WHERE TemplateId = @TemplateId", new { TemplateId = id });
        }
    }
}
