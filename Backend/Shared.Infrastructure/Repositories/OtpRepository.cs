using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Shared.Infrastructure.Repositories
{
    public interface IOtpRepository
    {
        Task SaveOtpAsync(string email, string otp, DateTime expiry);
        Task<(string Otp, DateTime Expiry)?> GetOtpAsync(string email);
        Task DeleteOtpAsync(string email);
    }

    public class OtpRepository : IOtpRepository
    {
        private readonly string _connectionString;

        public OtpRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? throw new ArgumentNullException("Connection string not found!");
        }

        public async Task SaveOtpAsync(string email, string otp, DateTime expiry)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                MERGE INTO OtpStore AS target
                USING (SELECT @Email as Email) AS source
                ON target.Email = source.Email
                WHEN MATCHED THEN
                    UPDATE SET OtpCode = @OtpCode, Expiry = @Expiry, CreatedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (Email, OtpCode, Expiry, CreatedAt)
                    VALUES (@Email, @OtpCode, @Expiry, GETUTCDATE());";

            await db.ExecuteAsync(query, new { Email = email, OtpCode = otp, Expiry = expiry });
        }

        public async Task<(string Otp, DateTime Expiry)?> GetOtpAsync(string email)
        {
            using var db = new SqlConnection(_connectionString);
            var result = await db.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT OtpCode, Expiry FROM OtpStore WHERE Email = @Email",
                new { Email = email });

            if (result == null) return null;
            return (result.OtpCode, result.Expiry);
        }

        public async Task DeleteOtpAsync(string email)
        {
            using var db = new SqlConnection(_connectionString);
            await db.ExecuteAsync("DELETE FROM OtpStore WHERE Email = @Email", new { Email = email });
        }
    }
}
