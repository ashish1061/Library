namespace Library.API.Services.Interfaces;

public interface IReportService
{
    Task<string> GetBooksReportCsvAsync();
    Task<string> GetActiveIssuesReportCsvAsync();
    Task<string> GetIssueHistoryReportCsvAsync(DateTime? startDate, DateTime? endDate);
}
