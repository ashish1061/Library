using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Shared.Infrastructure.Repositories;
using Shared.Core.Interfaces;

namespace Operations.Service.Services
{
    public class OverdueReminderService
    {
        private readonly IIssueRepository _issueRepository;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IEmailTemplateRepository _templateRepository;

        public OverdueReminderService(IIssueRepository issueRepository, IBackgroundJobClient backgroundJobClient, IEmailTemplateRepository templateRepository)
        {
            _issueRepository = issueRepository;
            _backgroundJobClient = backgroundJobClient;
            _templateRepository = templateRepository;
        }

        public async Task SendRemindersAsync()
        {
            var overdueIssues = await _issueRepository.GetOverdueIssuesAsync();

            if (overdueIssues == null || !overdueIssues.Any())
            {
                Console.WriteLine("No overdue issues found for today.");
                return;
            }

            var template = await _templateRepository.GetTemplateByPurposeAsync("Overdue Reminder");

            foreach (var issue in overdueIssues)
            {
                var recipientEmail = $"{issue.EmpID}@corp.com"; // Defaulting email
                var subject = template?.Subject ?? $"OVERDUE: {issue.BookName}";
                var body = template?.Body ?? $"Hello {issue.EmpName},\n\nYour borrowed book '{issue.BookName}' is OVERDUE.\nPlease return it immediately.\nDue Date was: {issue.DueDate}\n\nThanks,\nLibrary Admin";

                body = body.Replace("{{EmployeeName}}", issue.EmpName)
                           .Replace("{{BookName}}", issue.BookName)
                           .Replace("{{DueDate}}", issue.DueDate?.ToString("dd-MM-yyyy") ?? "");
                subject = subject.Replace("{{EmployeeName}}", issue.EmpName).Replace("{{BookName}}", issue.BookName);

                // Log email to database
                await _issueRepository.LogEmailAsync(issue.EmpID, recipientEmail, subject, body, issue.IssueNumber);

                _backgroundJobClient.Enqueue<IEmailService>(emailService => 
                    emailService.SendEmailAsync(
                        recipientEmail,
                        subject,
                        body
                    )
                );
            }
        }
    }
}
