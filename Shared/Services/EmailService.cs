using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts;
using Shared.Models;

namespace Shared.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly ISmtpClientWrapper _smtpClientWrapper;
    private readonly EmailConfiguration _emailConfiguration;

    public EmailService(ILogger<EmailService> logger, ISmtpClientWrapper smtpClientWrapper,
        IOptions<EmailConfiguration> emailConfiguration)
    {
        _logger = logger;
        _emailConfiguration = emailConfiguration.Value ?? throw new ArgumentNullException(nameof(emailConfiguration));

        if (string.IsNullOrEmpty(_emailConfiguration.SmtpServer))
            throw new ArgumentNullException(nameof(_emailConfiguration.SmtpServer));
        if (string.IsNullOrEmpty(_emailConfiguration.SenderEmail))
            throw new ArgumentNullException(nameof(_emailConfiguration.SenderEmail));
        if (string.IsNullOrEmpty(_emailConfiguration.SenderUsername))
            throw new ArgumentNullException(nameof(_emailConfiguration.SenderUsername));
        if (string.IsNullOrEmpty(_emailConfiguration.SenderPassword))
            throw new ArgumentNullException(nameof(_emailConfiguration.SenderPassword));
        if (string.IsNullOrEmpty(_emailConfiguration.RecipientEmail))
            throw new ArgumentNullException(nameof(_emailConfiguration.RecipientEmail));

        _smtpClientWrapper = smtpClientWrapper;
    }

    public async Task SendReturnNotificationEmailAsync()
    {
        try
        {
            var subject = "Aldi Retouren";
            var body = "Es wurden neue Retouren im Aldi Portal abgerufen.";

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(_emailConfiguration.SenderEmail);
                message.To.Add(_emailConfiguration.RecipientEmail);
                message.Subject = subject;
                message.Body = body;

                await _smtpClientWrapper.SendMailAsync(message);

                _logger.LogInformation("Die Email für die Retoureninformation wurde erfolgreich versendet.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Senden der Retoureninformations Email.");
        }
    }
}