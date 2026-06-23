using Library.API.DTOs;
using Library.API.Models;
using Library.API.Repositories.Interfaces;
using Library.API.Services.Interfaces;

namespace Library.API.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    public AuthService(IAuthRepository repo) => _repo = repo;

    public async Task<string> AuthenticateUserAsync(string username, string password)
    {
        var user = await _repo.GetUserByUsernameAsync(username);
        // Warning: using plaintext password matching for mockup purposes
        if (user == null || user.PasswordHash != password) return string.Empty;
        
        // Return dummy token, should be generating proper JWT here
        return "dummy-jwt-token";
    }

    public async Task<bool> RegisterUserAsync(string username, string password, string role)
    {
        return await _repo.CreateUserAsync(new User { Username = username, PasswordHash = password, Role = role });
    }
}

public class BookService : IBookService
{
    private readonly IBookRepository _repo;
    public BookService(IBookRepository repo) => _repo = repo;

    public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
    {
        var books = await _repo.GetAllBooksAsync();
        return books.Select(b => new BookDto { Anum = b.Anum, BookName = b.BookName, Author = b.Author, Publisher = b.Publisher, Quantity = b.Quantity });
    }

    public async Task<BookDto?> GetBookByAnumAsync(long anum)
    {
        var b = await _repo.GetBookByAnumAsync(anum);
        if (b == null) return null;
        return new BookDto { Anum = b.Anum, BookName = b.BookName, Author = b.Author, Publisher = b.Publisher, Quantity = b.Quantity };
    }

    public async Task<bool> AddBookAsync(BookDto b) => await _repo.AddBookAsync(new Book { Anum = b.Anum, BookName = b.BookName, Author = b.Author, Publisher = b.Publisher, Quantity = b.Quantity });
    public async Task<bool> UpdateBookAsync(BookDto b) => await _repo.UpdateBookAsync(new Book { Anum = b.Anum, BookName = b.BookName, Author = b.Author, Publisher = b.Publisher, Quantity = b.Quantity });
    public async Task<bool> DeleteBookAsync(long anum) => await _repo.DeleteBookAsync(anum);
}

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repo;
    public EmployeeService(IEmployeeRepository repo) => _repo = repo;

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
    {
        var emps = await _repo.GetAllEmployeesAsync();
        return emps.Select(e => new EmployeeDto { EmpID = e.EmpID, EmpName = e.EmpName, Department = e.Department, Email = e.Email });
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(string empId)
    {
        var e = await _repo.GetEmployeeByIdAsync(empId);
        if (e == null) return null;
        return new EmployeeDto { EmpID = e.EmpID, EmpName = e.EmpName, Department = e.Department, Email = e.Email };
    }

    public async Task<bool> AddEmployeeAsync(EmployeeDto e) => await _repo.AddEmployeeAsync(new Employee { EmpID = e.EmpID, EmpName = e.EmpName, Department = e.Department, Email = e.Email });
    public async Task<bool> UpdateEmployeeAsync(EmployeeDto e) => await _repo.UpdateEmployeeAsync(new Employee { EmpID = e.EmpID, EmpName = e.EmpName, Department = e.Department, Email = e.Email });
    public async Task<bool> DeleteEmployeeAsync(string empId) => await _repo.DeleteEmployeeAsync(empId);
}

public class IssueService : IIssueService
{
    private readonly IIssueRepository _repo;
    public IssueService(IIssueRepository repo) => _repo = repo;

    public async Task<IEnumerable<IssueDto>> GetAllIssuesAsync()
    {
        var issues = await _repo.GetAllIssuesAsync();
        return issues.Select(i => new IssueDto { IssueNumber = i.IssueNumber, Anum = i.Anum, BookName = i.BookName, EmpID = i.EmpID, EmpName = i.EmpName, IssueDate = i.IssueDate });
    }

    public async Task<bool> CreateIssueAsync(IssueDto i) => await _repo.CreateIssueAsync(new Issue { Anum = i.Anum, BookName = i.BookName, EmpID = i.EmpID, EmpName = i.EmpName, IssueDate = i.IssueDate });
    public async Task<bool> ReturnIssueAsync(int issueNumber) => await _repo.ReturnIssueAsync(issueNumber);
}

public class MagazineService : IMagazineService
{
    private readonly IMagazineRepository _repo;
    public MagazineService(IMagazineRepository repo) => _repo = repo;

    public async Task<IEnumerable<MagazineDto>> GetAllMagazinesAsync()
    {
        var mags = await _repo.GetAllMagazinesAsync();
        return mags.Select(m => new MagazineDto { Id = m.Id, Title = m.Title, IssueMonth = m.IssueMonth });
    }

    public async Task<bool> AddMagazineAsync(MagazineDto m) => await _repo.AddMagazineAsync(new Magazine { Title = m.Title, IssueMonth = m.IssueMonth });
}
