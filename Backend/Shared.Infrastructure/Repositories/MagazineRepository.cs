using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using Shared.Core.Domain;
using Shared.Core.Interfaces;

namespace Shared.Infrastructure.Repositories
{
    public class MagazineRepository : IMagazineRepository
    {
        private readonly string _connectionString;

        public MagazineRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? "";
        }

        public async Task<IEnumerable<Magazine>> GetAllMagazinesAsync()
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<Magazine>("SELECT * FROM Magazines");
        }

        public async Task<Magazine?> GetMagazineByIdAsync(long magazineId)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<Magazine>(
                "SELECT * FROM Magazines WHERE MagazineId = @MagazineId",
                new { MagazineId = magazineId });
        }

        public async Task<int> AddMagazineAsync(Magazine magazine)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                IF EXISTS (SELECT 1 FROM Magazines WHERE MagazineId = @MagazineId)
                BEGIN
                    UPDATE Magazines SET 
                        Title = @Title,
                        Publisher = @Publisher,
                        IssueDate = @IssueDate,
                        RackLocation = @RackLocation,
                        Category = @Category,
                        TotalCopies = @TotalCopies,
                        AvailableCopies = CASE WHEN @TotalCopies = 0 THEN 0 ELSE @AvailableCopies END,
                        CoverImagePath = CASE WHEN ISNULL(@CoverImagePath, '') = '' THEN CoverImagePath ELSE @CoverImagePath END
                    WHERE MagazineId = @MagazineId;
                END
                ELSE
                BEGIN
                    INSERT INTO Magazines (MagazineId, Title, Publisher, IssueDate, RackLocation, Category, TotalCopies, AvailableCopies, CoverImagePath)
                    VALUES (@MagazineId, @Title, @Publisher, @IssueDate, @RackLocation, @Category, @TotalCopies, CASE WHEN @TotalCopies = 0 THEN 0 ELSE @AvailableCopies END, @CoverImagePath);
                END
            ";
            return await db.ExecuteAsync(query, magazine);
        }

        public async Task<int> UpdateMagazineAsync(Magazine magazine)
        {
            return await AddMagazineAsync(magazine); // Upsert logic used in Add
        }

        public async Task<int> DeleteMagazineAsync(long magazineId)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.ExecuteAsync("DELETE FROM Magazines WHERE MagazineId = @MagazineId", new { MagazineId = magazineId });
        }

        public async Task<IEnumerable<Magazine>> SearchMagazinesAsync(string category, string keyword)
        {
            using var db = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Magazines WHERE 1=1";
            
            if (!string.IsNullOrEmpty(category))
            {
                query += " AND Category = @Category";
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                query += " AND (Title LIKE @Keyword OR Publisher LIKE @Keyword OR IssueDate LIKE @Keyword)";
            }

            return await db.QueryAsync<Magazine>(query, new { Category = category, Keyword = $"%{keyword}%" });
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<string>("SELECT DISTINCT Category FROM Magazines WHERE Category IS NOT NULL AND Category <> ''");
        }
    }
}
