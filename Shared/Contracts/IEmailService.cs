namespace Shared.Contracts;

public interface IEmailService
{
    Task SendReturnNotificationEmailAsync();
}