namespace Library.API.DTOs;

public class DashboardSummaryDto
{
    public int TotalBooks { get; set; }
    public int ActiveIssues { get; set; }
    public int RegisteredMembers { get; set; }
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
