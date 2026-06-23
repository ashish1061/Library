using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Shared.Core.Interfaces;
using System;

namespace Shared.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var emailConfig = _configuration.GetSection("EmailConfiguration");
            var smtpServer = emailConfig["SmtpServer"];
            var port = int.Parse(emailConfig["Port"] ?? "587");
            var username = emailConfig["Username"];
            var password = emailConfig["Password"];
            var fromEmail = emailConfig["FromEmail"] ?? username;

            // Skip if not configured or still using placeholder values
            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username)
                || username!.Contains("yourdomain.com") || username.Contains("your_email"))
            {
                Console.WriteLine($"[Email] SMTP not configured (placeholder credentials). Skipping email to {to}.");
                return;
            }

            using var client = new SmtpClient(smtpServer, port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true,
                Timeout = 120000 // 120 seconds to guarantee Office365 has enough time
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail!),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(to);

            // We allow the exception to bubble up so the API fails properly
            // and the UI can show a clear error message instead of a false success.
            await client.SendMailAsync(mailMessage);
        }
    }
}
