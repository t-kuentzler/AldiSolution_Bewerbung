using System.Net.Mail;
using Shared.Contracts;

namespace Shared.Wrapper;

public class SmtpClientWrapper : ISmtpClientWrapper
{
    private readonly SmtpClient _smtpClient;

    public SmtpClientWrapper(SmtpClient smtpClient)
    {
        _smtpClient = smtpClient;
    }

    public Task SendMailAsync(MailMessage message)
    {
        return _smtpClient.SendMailAsync(message);
    }
}