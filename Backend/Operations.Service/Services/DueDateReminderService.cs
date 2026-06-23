using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Repositories;
using Shared.Core.Interfaces;
namespace Operations.Service.Services
{
    public class DueDateReminderService : BackgroundService
    {
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;

        public DueDateReminderService(IEmailService emailService, IServiceScopeFactory scopeFactory)
        {
            _emailService = emailService;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var _issueRepository = scope.ServiceProvider.GetRequiredService<IIssueRepository>();
                    var _templateRepository = scope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();

                    var activeIssues = await _issueRepository.GetActiveIssuesAsync();
                    var upcomingTemplate = await _templateRepository.GetTemplateByPurposeAsync("Upcoming Due Date");
                    var overdueTemplate = await _templateRepository.GetTemplateByPurposeAsync("Overdue Reminder");

                    foreach (var issue in activeIssues)
                    {
                        if (issue.DueDate.HasValue)
                        {
                            var daysLeft = Math.Floor((issue.DueDate.Value - DateTime.Now).TotalDays);

                            if (daysLeft == 7 || daysLeft == 3 || daysLeft == 1)
                            {
                                var subject = upcomingTemplate?.Subject ?? $"Reminder: Book Due Soon - {issue.BookName}";
                                var body = upcomingTemplate?.Body ?? $"Hello {issue.EmpName},\n\nYour book '{issue.BookName}' is due on {issue.DueDate.Value.ToShortDateString()}.\nPlease return or reissue it to avoid penalties.\n\nThanks,\nLibrary Admin";
                                body = body.Replace("{{EmployeeName}}", issue.EmpName)
                                           .Replace("{{BookName}}", issue.BookName)
                                           .Replace("{{DueDate}}", issue.DueDate?.ToString("dd-MM-yyyy") ?? "");
                                subject = subject.Replace("{{EmployeeName}}", issue.EmpName).Replace("{{BookName}}", issue.BookName);

                                await _emailService.SendEmailAsync($"{issue.EmpID}@corp.com", subject, body);
                            }
                            else if (daysLeft < 0 && daysLeft >= -7) // Remind overdue for a week
                            {
                                var subject = overdueTemplate?.Subject ?? $"URGENT: Book Overdue - {issue.BookName}";
                                var body = overdueTemplate?.Body ?? $"Hello {issue.EmpName},\n\nYour book '{issue.BookName}' was due on {issue.DueDate.Value.ToShortDateString()} and is now OVERDUE.\nPlease return it immediately.\n\nThanks,\nLibrary Admin";
                                body = body.Replace("{{EmployeeName}}", issue.EmpName)
                                           .Replace("{{BookName}}", issue.BookName)
                                           .Replace("{{DueDate}}", issue.DueDate?.ToString("dd-MM-yyyy") ?? "");
                                subject = subject.Replace("{{EmployeeName}}", issue.EmpName).Replace("{{BookName}}", issue.BookName);

                                await _emailService.SendEmailAsync($"{issue.EmpID}@corp.com", subject, body);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reminder Service Error: {ex.Message}");
                }

                // Wait 24 hours before running again (simulated 1 minute for testing purposes)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
