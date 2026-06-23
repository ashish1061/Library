using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Operations.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly string _connectionString;

        public AnalyticsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? throw new ArgumentNullException("Connection string not found!");
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            using var db = new SqlConnection(_connectionString);
            var summary = new DashboardSummaryDto
            {
                TotalBooks = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Books"),
                ActiveIssues = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Issue"),
                RegisteredMembers = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employee"),
                TotalIssues = await db.ExecuteScalarAsync<int>("SELECT (SELECT COUNT(*) FROM dbo.Issue) + (SELECT COUNT(*) FROM dbo.IssueHistory)")
            };
            return Ok(summary);
        }

        [HttpGet("books-by-category")]
        public async Task<IActionResult> GetBooksByCategory()
        {
            using var db = new SqlConnection(_connectionString);
            var sql = @"
                SELECT ISNULL(Book_category, 'Uncategorized') as Category, COUNT(*) as Count 
                FROM dbo.Books 
                GROUP BY ISNULL(Book_category, 'Uncategorized')
                ORDER BY Count DESC";
            var result = await db.QueryAsync<CategoryDistributionDto>(sql);
            return Ok(result);
        }

        [HttpGet("issue-trends")]
        public async Task<IActionResult> GetIssueTrends()
        {
            using var db = new SqlConnection(_connectionString);
            // Fetch all dates and process in memory due to legacy string format DD-MM-YYYY HH:mm:ss
            var sql = @"
                SELECT IssueDate FROM dbo.Issue WHERE IssueDate IS NOT NULL
                UNION ALL
                SELECT IssueDate FROM dbo.IssueHistory WHERE IssueDate IS NOT NULL";
            
            var dates = await db.QueryAsync<string>(sql);
            
            var trends = dates
                .Where(d => d.Length >= 10)
                .Select(d => {
                    var parts = d.Substring(0, 10).Split('-');
                    if (parts.Length == 3) {
                        return $"{parts[2]}-{parts[1]}";
                    }
                    return "Unknown";
                })
                .Where(m => m != "Unknown")
                .GroupBy(m => m)
                .Select(g => new IssueTrendDto { Month = g.Key, IssueCount = g.Count() })
                .OrderBy(t => t.Month)
                .TakeLast(6)
                .ToList();

            return Ok(trends);
        }
    }

    public class DashboardSummaryDto
    {
        public int TotalBooks { get; set; }
        public int ActiveIssues { get; set; }
        public int RegisteredMembers { get; set; }
        public int TotalIssues { get; set; }
    }

    public class CategoryDistributionDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class IssueTrendDto
    {
        public string Month { get; set; } = string.Empty;
        public int IssueCount { get; set; }
    }
}
