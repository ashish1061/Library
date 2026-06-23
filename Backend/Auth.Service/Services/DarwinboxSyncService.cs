using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Domain;
using Shared.Infrastructure.Repositories;
using System.Linq;

namespace Auth.Service.Services
{
    public class DarwinboxSyncService : BackgroundService
    {
        private readonly ILogger<DarwinboxSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DarwinboxSyncService(ILogger<DarwinboxSyncService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DarwinboxSyncService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var next11PM = now.Date.AddHours(23); // 11 PM today
                if (now > next11PM)
                {
                    next11PM = next11PM.AddDays(1); // 11 PM tomorrow
                }

                var delay = next11PM - now;
                _logger.LogInformation($"Next Darwinbox sync scheduled in {delay.TotalHours:F2} hours (at {next11PM}).");

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("DarwinboxSyncService is syncing employee data...");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
                        var darwinboxService = scope.ServiceProvider.GetRequiredService<Shared.Core.Interfaces.IDarwinboxService>();

                        var employees = await darwinboxService.GetEmployeesAsync();
                        if (employees != null && employees.Any())
                        {
                            var rowsAffected = await employeeRepository.UpsertEmployeesAsync(employees);
                            _logger.LogInformation($"Successfully synchronized with Darwinbox. Rows affected: {rowsAffected}");
                        }
                        else
                        {
                            _logger.LogWarning("Darwinbox API returned no employees during auto-sync.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Darwinbox auto sync at 11 PM.");
                }
            }
        }

        private class DummyUser
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }
    }
}
