using MailKit.Net.Smtp;
using MimeKit;

namespace Backend.Services
{
    public interface IEmailService
    {
        Task SendInvoiceEmailAsync(string toEmail, string customerName, int invoiceId, decimal amount, string invoiceDetails);
        Task SendCreditReminderAsync(string toEmail, string customerName, decimal amount, int daysOverdue);
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

        public async Task SendInvoiceEmailAsync(string toEmail, string customerName, int invoiceId, decimal amount, string invoiceDetails)
        {
            var subject = $"Invoice #{invoiceId} - Vehicle Service Center";
            var body = $@"
<html><body style='font-family:Arial,sans-serif;'>
<h2 style='color:#1e40af;'>Vehicle Service Center</h2>
<p>Dear {customerName},</p>
<p>Please find your invoice details below:</p>
<div style='background:#f1f5f9;padding:15px;border-radius:8px;'>
<p><strong>Invoice #:</strong> {invoiceId}</p>
<p><strong>Amount:</strong> Rs. {amount:N2}</p>
{invoiceDetails}
</div>
<p>Thank you for your business!</p>
<hr/><p style='color:#64748b;font-size:12px;'>Vehicle Parts & Service Center</p>
</body></html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendCreditReminderAsync(string toEmail, string customerName, decimal amount, int daysOverdue)
        {
            var subject = "Payment Reminder - Vehicle Service Center";
            var body = $@"
<html><body style='font-family:Arial,sans-serif;'>
<h2 style='color:#dc2626;'>Payment Reminder</h2>
<p>Dear {customerName},</p>
<p>This is a reminder that you have an outstanding credit balance of <strong>Rs. {amount:N2}</strong> which is <strong>{daysOverdue} days</strong> overdue.</p>
<p>Please settle your balance at your earliest convenience.</p>
<p>Thank you,<br/>Vehicle Parts & Service Center</p>
</body></html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["Email:From"] ?? "noreply@vehicleims.com"));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

                using var smtp = new SmtpClient();
                var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
                var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
                await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                var user = _config["Email:Username"];
                var pass = _config["Email:Password"];
                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                    await smtp.AuthenticateAsync(user, pass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}
