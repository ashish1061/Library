using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shared.Core.Domain;

namespace Shared.Infrastructure.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task SaveRefreshTokenAsync(UserRefreshToken token);
        Task<UserRefreshToken?> GetRefreshTokenAsync(string token);
        Task UpdateRefreshTokenAsync(UserRefreshToken token);
        Task RevokeAllTokensForUserAsync(string empId);
    }

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly string _connectionString;

        public RefreshTokenRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? throw new ArgumentNullException("Connection string not found!");
        }

        public async Task SaveRefreshTokenAsync(UserRefreshToken token)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                INSERT INTO UserRefreshTokens (EmpID, Token, Expiry, IsRevoked, CreatedAt, RevokedAt, ReplacedByToken)
                VALUES (@EmpID, @Token, @Expiry, @IsRevoked, GETUTCDATE(), @RevokedAt, @ReplacedByToken);";

            await db.ExecuteAsync(query, new {
                EmpID = token.EmpID,
                Token = token.Token,
                Expiry = token.Expiry,
                IsRevoked = token.IsRevoked,
                RevokedAt = token.RevokedAt,
                ReplacedByToken = token.ReplacedByToken
            });
        }

        public async Task<UserRefreshToken?> GetRefreshTokenAsync(string token)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<UserRefreshToken>(
                "SELECT * FROM UserRefreshTokens WHERE Token = @Token",
                new { Token = token });
        }

        public async Task UpdateRefreshTokenAsync(UserRefreshToken token)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                UPDATE UserRefreshTokens
                SET IsRevoked = @IsRevoked,
                    RevokedAt = @RevokedAt,
                    ReplacedByToken = @ReplacedByToken
                WHERE Id = @Id;";

            await db.ExecuteAsync(query, token);
        }

        public async Task RevokeAllTokensForUserAsync(string empId)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                UPDATE UserRefreshTokens
                SET IsRevoked = 1,
                    RevokedAt = GETUTCDATE()
                WHERE EmpID = @EmpID AND IsRevoked = 0;";

            await db.ExecuteAsync(query, new { EmpID = empId });
        }
    }
}
