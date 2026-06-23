using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shared.Core.Domain;

namespace Shared.Infrastructure.Repositories
{
    public interface IIssueRepository
    {
        Task<IEnumerable<Issue>> GetActiveIssuesAsync();
        Task<IEnumerable<IssueHistory>> GetIssueHistoryAsync();
        Task<int> IssueBookAsync(Issue issue);
        Task<int> ReturnBookAsync(int issueNumber, string returnDate);
        Task<int> ReissueBookAsync(int issueNumber);
        Task<IEnumerable<Issue>> GetActiveIssuesByAnumAsync(long anum);
        Task<IEnumerable<Issue>> GetOverdueIssuesAsync();
        Task LogEmailAsync(string empId, string recipientEmail, string subject, string body, int? issueNumber);
        
        // Issue Requests
        Task<int> CreateRequestAsync(IssueRequest request);
        Task<IEnumerable<IssueRequest>> GetPendingRequestsAsync();
        Task<IEnumerable<IssueRequest>> GetAllRequestsAsync();
        Task<IssueRequest?> GetRequestByIdAsync(int requestId);
        Task<int> UpdateRequestStatusAsync(int requestId, string status);
        Task<string?> GetEmployeeEmailAsync(string empId);
        Task<Employee?> GetEmployeeByEmailAsync(string email);
    }

    public class IssueRepository : IIssueRepository
    {
        private readonly string _connectionString;

        public IssueRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? throw new ArgumentNullException("Connection string not found!");
        }

        public async Task<string?> GetEmployeeEmailAsync(string empId)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<string>("SELECT emailid FROM Employee WHERE EmpID = @EmpID", new { EmpID = empId });
        }

        public async Task<IEnumerable<Issue>> GetOverdueIssuesAsync()
        {
            using var db = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Issue WHERE DueDate < GETDATE()";
            return await db.QueryAsync<Issue>(query);
        }

        public async Task<IEnumerable<Issue>> GetActiveIssuesAsync()
        {
            using var db = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Issue";
            return await db.QueryAsync<Issue>(query);
        }

        public async Task<IEnumerable<Issue>> GetActiveIssuesByAnumAsync(long anum)
        {
            using var db = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Issue WHERE Anum = @Anum";
            return await db.QueryAsync<Issue>(query, new { Anum = anum });
        }

        public async Task<IEnumerable<IssueHistory>> GetIssueHistoryAsync()
        {
            using var db = new SqlConnection(_connectionString);
            var query = "SELECT * FROM IssueHistory";
            return await db.QueryAsync<IssueHistory>(query);
        }

        public async Task<int> IssueBookAsync(Issue issue)
        {
            using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();
            using var transaction = await db.BeginTransactionAsync();
            try
            {
                issue.DueDate = DateTime.Now.AddDays(14);
                if (string.IsNullOrEmpty(issue.ItemType)) issue.ItemType = "Book";

                var query = @"
                    INSERT INTO Issue (Anum, BookName, EmpID, EmpName, IssueDate, ISemp, DueDate, ReissueCount, ItemType)
                    VALUES (@Anum, @BookName, @EmpID, @EmpName, @IssueDate, @ISemp, @DueDate, 0, @ItemType);
                    
                    IF @ItemType = 'Magazine'
                        UPDATE Magazines SET TotalCopies = TotalCopies - 1, AvailableCopies = AvailableCopies - 1 WHERE MagazineId = @Anum AND TotalCopies > 0;
                    ELSE
                        UPDATE Books SET TotalCopies = TotalCopies - 1 WHERE Anum = @Anum AND TotalCopies > 0;
                    
                    SELECT CAST(SCOPE_IDENTITY() as int);
                ";
                var result = await db.ExecuteScalarAsync<int>(query, issue, transaction);

                await db.ExecuteAsync("INSERT INTO AuditLogs (Action, EmpID, Entity, EntityId, Details) VALUES ('Issue', @EmpID, @ItemType, @Anum, 'Issued item')", new { issue.EmpID, ItemType = issue.ItemType, issue.Anum }, transaction);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> ReturnBookAsync(int issueNumber, string returnDate)
        {
            using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();
            using var transaction = await db.BeginTransactionAsync();
            try
            {
                var issue = await db.QuerySingleOrDefaultAsync<Issue>("SELECT * FROM Issue WHERE IssueNumber = @IssueNumber", new { IssueNumber = issueNumber }, transaction);
                if (issue != null)
                {
                    var query = @"
                        INSERT INTO IssueHistory (Anum, BookName, EmpID, EmpName, IssueDate, ReturnDate, ISemp, DueDate, ReissueCount, ItemType)
                        VALUES (@Anum, @BookName, @EmpID, @EmpName, @IssueDate, @ReturnDate, @ISemp, @DueDate, @ReissueCount, @ItemType);
                        
                        DELETE FROM Issue WHERE IssueNumber = @IssueNumber;
                        
                        IF @ItemType = 'Magazine'
                            UPDATE Magazines SET TotalCopies = TotalCopies + 1, AvailableCopies = AvailableCopies + 1 WHERE MagazineId = @Anum;
                        ELSE
                            UPDATE Books SET TotalCopies = TotalCopies + 1 WHERE Anum = @Anum;
                    ";
                    await db.ExecuteAsync(query, new { issue.Anum, issue.BookName, issue.EmpID, issue.EmpName, issue.IssueDate, ReturnDate = returnDate, issue.ISemp, issue.DueDate, issue.ReissueCount, ItemType = issue.ItemType, IssueNumber = issueNumber }, transaction);
                    
                    await db.ExecuteAsync("INSERT INTO AuditLogs (Action, EmpID, Entity, EntityId, Details) VALUES ('Return', @EmpID, @ItemType, @Anum, 'Returned item')", new { issue.EmpID, ItemType = issue.ItemType, issue.Anum }, transaction);
                    await transaction.CommitAsync();
                    return 1;
                }
                await transaction.CommitAsync();
                return 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> ReissueBookAsync(int issueNumber)
        {
            using var db = new SqlConnection(_connectionString);
            var issue = await db.QuerySingleOrDefaultAsync<Issue>("SELECT * FROM Issue WHERE IssueNumber = @IssueNumber", new { IssueNumber = issueNumber });
            if (issue != null)
            {
                var newDueDate = DateTime.Now.AddDays(14);
                var query = "UPDATE Issue SET DueDate = @NewDueDate, ReissueCount = ReissueCount + 1 WHERE IssueNumber = @IssueNumber";
                await db.ExecuteAsync(query, new { NewDueDate = newDueDate, IssueNumber = issueNumber });

                await db.ExecuteAsync("INSERT INTO AuditLogs (Action, EmpID, Entity, EntityId, Details) VALUES ('Reissue', @EmpID, 'Book', @Anum, 'Reissued book')", new { issue.EmpID, issue.Anum });
                return 1;
            }
            return 0;
        }

        public async Task LogEmailAsync(string empId, string recipientEmail, string subject, string body, int? issueNumber)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                INSERT INTO EmailLogs (EmpID, RecipientEmail, Subject, Body, SentAt, IssueNumber)
                VALUES (@EmpID, @RecipientEmail, @Subject, @Body, GETDATE(), @IssueNumber)";
            
            await db.ExecuteAsync(query, new { 
                EmpID = empId, 
                RecipientEmail = recipientEmail, 
                Subject = subject, 
                Body = body, 
                IssueNumber = issueNumber 
            });
        }

        public async Task<int> CreateRequestAsync(IssueRequest request)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                INSERT INTO IssueRequests (EmpID, ItemType, ItemID, ItemName, RequestDate, Status)
                VALUES (@EmpID, @ItemType, @ItemID, @ItemName, @RequestDate, @Status);
                SELECT CAST(SCOPE_IDENTITY() as int);";
            return await db.ExecuteScalarAsync<int>(query, request);
        }

        public async Task<IEnumerable<IssueRequest>> GetPendingRequestsAsync()
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                SELECT r.*, e.EmpName 
                FROM IssueRequests r 
                LEFT JOIN Library.dbo.Employee e ON r.EmpID = e.EmpID 
                WHERE r.Status = 'Pending'";
            return await db.QueryAsync<IssueRequest>(query);
        }

        public async Task<IEnumerable<IssueRequest>> GetAllRequestsAsync()
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                SELECT r.*, e.EmpName 
                FROM IssueRequests r 
                LEFT JOIN Library.dbo.Employee e ON r.EmpID = e.EmpID";
            return await db.QueryAsync<IssueRequest>(query);
        }

        public async Task<IssueRequest?> GetRequestByIdAsync(int requestId)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                SELECT r.*, e.EmpName 
                FROM IssueRequests r 
                LEFT JOIN Library.dbo.Employee e ON r.EmpID = e.EmpID 
                WHERE r.RequestID = @RequestID";
            return await db.QuerySingleOrDefaultAsync<IssueRequest>(query, new { RequestID = requestId });
        }

        public async Task<int> UpdateRequestStatusAsync(int requestId, string status)
        {
            using var db = new SqlConnection(_connectionString);
            var query = "UPDATE IssueRequests SET Status = @Status WHERE RequestID = @RequestID";
            return await db.ExecuteAsync(query, new { Status = status, RequestID = requestId });
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<Employee>(
                "SELECT * FROM Employee WHERE emailid = @Email AND (IsDeleted = 0 OR IsDeleted IS NULL)",
                new { Email = email });
        }
    }
}
