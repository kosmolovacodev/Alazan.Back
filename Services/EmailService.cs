using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Alazan.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var section = _config.GetSection("Email");
            var host     = section["Host"]     ?? "smtp.gmail.com";
            var port     = int.Parse(section["Port"] ?? "587");
            var username = section["Username"] ?? "";
            var password = section["Password"] ?? "";
            var from     = section["From"]     ?? username;
            var fromName = section["FromName"] ?? "Sistema Alazan";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email enviado a {Email}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {Email}", toEmail);
                throw;
            }
        }
    }
}
