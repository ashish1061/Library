using Library.API.DTOs;

namespace Library.API.Repositories.Interfaces;

public interface IAnalyticsRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<IEnumerable<CategoryDistributionDto>> GetBooksByCategoryAsync();
    Task<IEnumerable<IssueTrendDto>> GetIssueTrendsAsync();
}
