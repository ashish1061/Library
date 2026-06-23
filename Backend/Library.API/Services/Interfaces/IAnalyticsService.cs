using Library.API.DTOs;

namespace Library.API.Services.Interfaces;

public interface IAnalyticsService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<IEnumerable<CategoryDistributionDto>> GetBooksByCategoryAsync();
    Task<IEnumerable<IssueTrendDto>> GetIssueTrendsAsync();
}
