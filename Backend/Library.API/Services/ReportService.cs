using System.Text;
using Library.API.Models;
using Library.API.Repositories.Interfaces;
using Library.API.Services.Interfaces;

namespace Library.API.Services;

public class ReportService : IReportService
{
    private readonly IBookRepository _bookRepo;
    private readonly IIssueRepository _issueRepo;

    public ReportService(IBookRepository bookRepo, IIssueRepository issueRepo)
    {
        _bookRepo = bookRepo;
        _issueRepo = issueRepo;
    }

    public async Task<string> GetBooksReportCsvAsync()
    {
        var books = await _bookRepo.GetAllBooksAsync();
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("AccessionNumber,BookName,Author,Publisher,Quantity");
        
        foreach (var book in books)
        {
            sb.AppendLine($"{book.Anum},{EscapeCsv(book.BookName)},{EscapeCsv(book.Author)},{EscapeCsv(book.Publisher)},{book.Quantity}");
        }
        
        return sb.ToString();
    }

    public async Task<string> GetActiveIssuesReportCsvAsync()
    {
        var issues = await _issueRepo.GetAllIssuesAsync();
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("IssueNumber,AccessionNumber,BookName,EmployeeID,EmployeeName,IssueDate");
        
        foreach (var issue in issues)
        {
            sb.AppendLine($"{issue.IssueNumber},{issue.Anum},{EscapeCsv(issue.BookName)},{EscapeCsv(issue.EmpID)},{EscapeCsv(issue.EmpName)},{EscapeCsv(issue.IssueDate)}");
        }
        
        return sb.ToString();
    }

    public async Task<string> GetIssueHistoryReportCsvAsync(DateTime? startDate, DateTime? endDate)
    {
        var history = await _issueRepo.GetIssueHistoryAsync(startDate, endDate);
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("IssueNumber,AccessionNumber,BookName,EmployeeID,EmployeeName,IssueDate,ReturnDate,DueDate,ReissueCount");
        
        foreach (var item in history)
        {
            sb.AppendLine($"{item.IssueNumber},{item.Anum},{EscapeCsv(item.BookName)},{EscapeCsv(item.EmpID)},{EscapeCsv(item.EmpName)},{EscapeCsv(item.IssueDate)},{EscapeCsv(item.ReturnDate)},{item.DueDate?.ToString("yyyy-MM-dd")},{item.ReissueCount}");
        }
        
        return sb.ToString();
    }
    
    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
            
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            // Escape quotes by doubling them
            value = value.Replace("\"", "\"\"");
            // Enclose in quotes
            return $"\"{value}\"";
        }
        
        return value;
    }
}
