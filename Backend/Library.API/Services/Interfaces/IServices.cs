using Library.API.DTOs;

namespace Library.API.Services.Interfaces;

public interface IBookService
{
    Task<IEnumerable<BookDto>> GetAllBooksAsync();
    Task<BookDto?> GetBookByAnumAsync(long anum);
    Task<bool> AddBookAsync(BookDto book);
    Task<bool> UpdateBookAsync(BookDto book);
    Task<bool> DeleteBookAsync(long anum);
}

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
    Task<EmployeeDto?> GetEmployeeByIdAsync(string empId);
    Task<bool> AddEmployeeAsync(EmployeeDto employee);
    Task<bool> UpdateEmployeeAsync(EmployeeDto employee);
    Task<bool> DeleteEmployeeAsync(string empId);
}

public interface IIssueService
{
    Task<IEnumerable<IssueDto>> GetAllIssuesAsync();
    Task<bool> CreateIssueAsync(IssueDto issue);
    Task<bool> ReturnIssueAsync(int issueNumber);
}

public interface IMagazineService
{
    Task<IEnumerable<MagazineDto>> GetAllMagazinesAsync();
    Task<bool> AddMagazineAsync(MagazineDto magazine);
}
