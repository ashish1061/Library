namespace Library.API.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class Book
{
    public long Anum { get; set; }
    public string BookName { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public int Quantity { get; set; }
}

public class Employee
{
    public string EmpID { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Email { get; set; }
}

public class Issue
{
    public int IssueNumber { get; set; }
    public long Anum { get; set; }
    public string BookName { get; set; } = string.Empty;
    public string EmpID { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
}

public class Magazine
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? IssueMonth { get; set; }
}

public class IssueHistory
{
    public int IssueNumber { get; set; }
    public long Anum { get; set; }
    public string BookName { get; set; } = string.Empty;
    public string EmpID { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int ReissueCount { get; set; }
}
