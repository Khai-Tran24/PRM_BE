using System.Net;
using System.Net.Mail;

namespace BE_SaleHunter.Application.Services
{
    public interface IEmailService 
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpClient = new SmtpClient(_config["Smtp:Host"])
            {
                Port = int.Parse(_config["Smtp:Port"]),
                Credentials = new NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
                EnableSsl = true,
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"]),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            try
            {
                await smtpClient.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send email: {Message}", ex.Message);
                throw;
            }
        }
    }
}
