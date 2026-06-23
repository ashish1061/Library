using System.Data;
using Dapper;
using Library.API.Models;
using Library.API.Repositories.Interfaces;

namespace Library.API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly IDbConnection _db;
    public AuthRepository(IDbConnection db) => _db = db;

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(
            "sp_ValidateUser", 
            new { Username = username }, 
            commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        var rows = await _db.ExecuteAsync(
            "sp_CreateUser", 
            new { user.Username, user.PasswordHash, user.Role }, 
            commandType: CommandType.StoredProcedure);
        return rows > 0;
    }
}

public class BookRepository : IBookRepository
{
    private readonly IDbConnection _db;
    public BookRepository(IDbConnection db) => _db = db;

    public async Task<IEnumerable<Book>> GetAllBooksAsync() => 
        await _db.QueryAsync<Book>("SELECT * FROM Books WHERE IsDeleted = 0");
        
    public async Task<Book?> GetBookByAnumAsync(long anum) => 
        await _db.QueryFirstOrDefaultAsync<Book>("SELECT * FROM Books WHERE Anum = @Anum AND IsDeleted = 0", new { Anum = anum });
        
    public async Task<bool> AddBookAsync(Book book) => 
        await _db.ExecuteAsync(@"
            INSERT INTO Books (Anum, Book_name, Book_author, Book_rack, Book_class, Book_category, Available, Publisher, ISBN, Edition, TotalCopies, CoverImagePath, IsDeleted) 
            VALUES (@Anum, @Book_name, @Book_author, @Book_rack, @Book_class, @Book_category, @Available, @Publisher, @ISBN, @Edition, @TotalCopies, @CoverImagePath, 0)", 
            book) > 0;
            
    public async Task<bool> UpdateBookAsync(Book book) => 
        await _db.ExecuteAsync(@"
            UPDATE Books SET 
                Book_name = @Book_name, Book_author = @Book_author, Book_rack = @Book_rack, Book_class = @Book_class, 
                Book_category = @Book_category, Available = @Available, Publisher = @Publisher, ISBN = @ISBN, 
                Edition = @Edition, TotalCopies = @TotalCopies, CoverImagePath = @CoverImagePath
            WHERE Anum = @Anum AND IsDeleted = 0", 
            book) > 0;
            
    public async Task<bool> DeleteBookAsync(long anum) => 
        await _db.ExecuteAsync("UPDATE Books SET IsDeleted = 1 WHERE Anum = @Anum", new { Anum = anum }) > 0;
}

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnection _db;
    public EmployeeRepository(IDbConnection db) => _db = db;

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync() => 
        await _db.QueryAsync<Employee>("SELECT * FROM Employee WHERE IsDeleted = 0");
        
    public async Task<Employee?> GetEmployeeByIdAsync(string empId) => 
        await _db.QueryFirstOrDefaultAsync<Employee>("SELECT * FROM Employee WHERE EmpID = @EmpID AND IsDeleted = 0", new { EmpID = empId });
        
    public async Task<bool> AddEmployeeAsync(Employee employee) => 
        await _db.ExecuteAsync(@"
            INSERT INTO Employee (EmpID, EmpName, password, emailid, mobile, Validity, Department, Designation, ImagePath, IsDeleted) 
            VALUES (@EmpID, @EmpName, @password, @emailid, @mobile, @Validity, @Department, @Designation, @ImagePath, 0)", 
            employee) > 0;
            
    public async Task<bool> UpdateEmployeeAsync(Employee employee) => 
        await _db.ExecuteAsync(@"
            UPDATE Employee SET 
                EmpName = @EmpName, password = @password, emailid = @emailid, mobile = @mobile, 
                Validity = @Validity, Department = @Department, Designation = @Designation, ImagePath = @ImagePath
            WHERE EmpID = @EmpID AND IsDeleted = 0", 
            employee) > 0;
            
    public async Task<bool> DeleteEmployeeAsync(string empId) => 
        await _db.ExecuteAsync("UPDATE Employee SET IsDeleted = 1 WHERE EmpID = @EmpID", new { EmpID = empId }) > 0;
}

public class IssueRepository : IIssueRepository
{
    private readonly IDbConnection _db;
    public IssueRepository(IDbConnection db) => _db = db;

    public async Task<IEnumerable<Issue>> GetAllIssuesAsync() => await _db.QueryAsync<Issue>("sp_GetIssues", commandType: CommandType.StoredProcedure);
    public async Task<bool> CreateIssueAsync(Issue issue) => await _db.ExecuteAsync("sp_CreateIssue", new { issue.Anum, issue.BookName, issue.EmpID, issue.EmpName, issue.IssueDate }, commandType: CommandType.StoredProcedure) > 0;
    public async Task<bool> ReturnIssueAsync(int issueNumber) => await _db.ExecuteAsync("sp_ReturnIssue", new { IssueNumber = issueNumber }, commandType: CommandType.StoredProcedure) > 0;
    
    public async Task<IEnumerable<IssueHistory>> GetIssueHistoryAsync(DateTime? startDate, DateTime? endDate)
    {
        return await _db.QueryAsync<IssueHistory>(
            "sp_GetIssueHistory",
            new { StartDate = startDate, EndDate = endDate },
            commandType: CommandType.StoredProcedure
        );
    }
}

public class MagazineRepository : IMagazineRepository
{
    private readonly IDbConnection _db;
    public MagazineRepository(IDbConnection db) => _db = db;

    public async Task<IEnumerable<Magazine>> GetAllMagazinesAsync() => await _db.QueryAsync<Magazine>("sp_GetMagazines", commandType: CommandType.StoredProcedure);
    public async Task<bool> AddMagazineAsync(Magazine magazine) => await _db.ExecuteAsync("sp_AddMagazine", magazine, commandType: CommandType.StoredProcedure) > 0;
}
