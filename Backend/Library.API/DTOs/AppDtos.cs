namespace Library.API.DTOs;

public class BookDto
{
    public long Anum { get; set; }
    public string BookName { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public int Quantity { get; set; }
}

public class EmployeeDto
{
    public string EmpID { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Email { get; set; }
}

public class IssueDto
{
    public int IssueNumber { get; set; }
    public long Anum { get; set; }
    public string BookName { get; set; } = string.Empty;
    public string EmpID { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
}

public class MagazineDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? IssueMonth { get; set; }
}
