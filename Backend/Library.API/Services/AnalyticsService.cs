using Library.API.DTOs;
using Library.API.Repositories.Interfaces;
using Library.API.Services.Interfaces;

namespace Library.API.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _repo;
    public AnalyticsService(IAnalyticsRepository repo) => _repo = repo;

    public async Task<DashboardSummaryDto> GetSummaryAsync() => await _repo.GetSummaryAsync();
    
    public async Task<IEnumerable<CategoryDistributionDto>> GetBooksByCategoryAsync() => await _repo.GetBooksByCategoryAsync();
    
    public async Task<IEnumerable<IssueTrendDto>> GetIssueTrendsAsync() => await _repo.GetIssueTrendsAsync();
}
