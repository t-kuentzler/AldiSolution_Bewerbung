using System.Net.Mail;

namespace Shared.Contracts;

public interface ISmtpClientWrapper
{
    Task SendMailAsync(MailMessage message);
}