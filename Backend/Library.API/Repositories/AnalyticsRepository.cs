using System.Data;
using Dapper;
using Library.API.DTOs;
using Library.API.Repositories.Interfaces;

namespace Library.API.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly IDbConnection _db;
    public AnalyticsRepository(IDbConnection db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var summary = new DashboardSummaryDto();
        summary.TotalBooks = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Books");
        summary.ActiveIssues = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Issue");
        summary.RegisteredMembers = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employee");
        return summary;
    }

    public async Task<IEnumerable<CategoryDistributionDto>> GetBooksByCategoryAsync()
    {
        var sql = @"
            SELECT ISNULL(Book_category, 'Uncategorized') as Category, COUNT(*) as Count 
            FROM dbo.Books 
            GROUP BY ISNULL(Book_category, 'Uncategorized')
            ORDER BY Count DESC";
        return await _db.QueryAsync<CategoryDistributionDto>(sql);
    }

    public async Task<IEnumerable<IssueTrendDto>> GetIssueTrendsAsync()
    {
        // Fetch all dates and process in memory due to legacy string format DD-MM-YYYY HH:mm:ss
        var sql = @"
            SELECT IssueDate FROM dbo.Issue WHERE IssueDate IS NOT NULL
            UNION ALL
            SELECT IssueDate FROM dbo.IssueHistory WHERE IssueDate IS NOT NULL";
        
        var dates = await _db.QueryAsync<string>(sql);
        
        var trends = dates
            .Where(d => d.Length >= 10)
            .Select(d => {
                // "11-12-2018 11:48:29" -> Month: "2018-12"
                var parts = d.Substring(0, 10).Split('-');
                if(parts.Length == 3) {
                    return $"{parts[2]}-{parts[1]}";
                }
                return "Unknown";
            })
            .Where(m => m != "Unknown")
            .GroupBy(m => m)
            .Select(g => new IssueTrendDto { Month = g.Key, IssueCount = g.Count() })
            .OrderBy(t => t.Month)
            .TakeLast(6) // Last 6 months that had activity
            .ToList();

        return trends;
    }
}
