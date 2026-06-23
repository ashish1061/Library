using Library.API.Models;

namespace Library.API.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> CreateUserAsync(User user);
}

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllBooksAsync();
    Task<Book?> GetBookByAnumAsync(long anum);
    Task<bool> AddBookAsync(Book book);
    Task<bool> UpdateBookAsync(Book book);
    Task<bool> DeleteBookAsync(long anum);
}

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    Task<Employee?> GetEmployeeByIdAsync(string empId);
    Task<bool> AddEmployeeAsync(Employee employee);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(string empId);
}

public interface IIssueRepository
{
    Task<IEnumerable<Issue>> GetAllIssuesAsync();
    Task<bool> CreateIssueAsync(Issue issue);
    Task<bool> ReturnIssueAsync(int issueNumber);
    Task<IEnumerable<IssueHistory>> GetIssueHistoryAsync(DateTime? startDate, DateTime? endDate);
}

public interface IMagazineRepository
{
    Task<IEnumerable<Magazine>> GetAllMagazinesAsync();
    Task<bool> AddMagazineAsync(Magazine magazine);
}
