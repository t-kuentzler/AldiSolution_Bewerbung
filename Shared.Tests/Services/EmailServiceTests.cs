using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Contracts;
using Shared.Models;
using Shared.Services;

namespace Shared.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Mock<ISmtpClientWrapper> _smtpClientWrapperMock;
    private readonly IOptions<EmailConfiguration> _emailConfiguration;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmailService>>();
        _smtpClientWrapperMock = new Mock<ISmtpClientWrapper>();

        var emailConfig = new EmailConfiguration
        {
            SmtpServer = "smtp.server.com",
            SenderEmail = "sender@example.com",
            SenderUsername = "username",
            SenderPassword = "password",
            RecipientEmail = "recipient@example.com",
            Port = 587
        };

        _emailConfiguration = Options.Create(emailConfig);
        _emailService = new EmailService(_loggerMock.Object, _smtpClientWrapperMock.Object, _emailConfiguration);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEmailConfigurationIsNull()
    {
        // Arrange
        var invalidEmailConfiguration = Options.Create<EmailConfiguration>(null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailService(_loggerMock.Object, _smtpClientWrapperMock.Object, invalidEmailConfiguration));
    }

    [Theory]
    [InlineData(null, "SmtpServer")]
    [InlineData("", "SmtpServer")]
    [InlineData(null, "SenderEmail")]
    [InlineData("", "SenderEmail")]
    [InlineData(null, "SenderUsername")]
    [InlineData("", "SenderUsername")]
    [InlineData(null, "SenderPassword")]
    [InlineData("", "SenderPassword")]
    [InlineData(null, "RecipientEmail")]
    [InlineData("", "RecipientEmail")]
    public void Constructor_ThrowsArgumentNullException_WhenRequiredConfigurationIsMissing(string value, string parameter)
    {
        // Arrange
        var emailConfig = new EmailConfiguration
        {
            SmtpServer = parameter == "SmtpServer" ? value : "smtp.server.com",
            SenderEmail = parameter == "SenderEmail" ? value : "sender@example.com",
            SenderUsername = parameter == "SenderUsername" ? value : "username",
            SenderPassword = parameter == "SenderPassword" ? value : "password",
            RecipientEmail = parameter == "RecipientEmail" ? value : "recipient@example.com",
            Port = 587
        };

        var config = Options.Create(emailConfig);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new EmailService(_loggerMock.Object, _smtpClientWrapperMock.Object, config));
        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public async Task SendReturnNotificationEmailAsync_SendsEmailSuccessfully()
    {
        // Arrange
        _smtpClientWrapperMock.Setup(client => client.SendMailAsync(It.Is<MailMessage>(
            m => m.Subject == "Aldi Retouren" && m.Body == "Es wurden neue Retouren im Aldi Portal abgerufen."
        ))).Returns(Task.CompletedTask);

        // Act
        await _emailService.SendReturnNotificationEmailAsync();

        // Assert
        _smtpClientWrapperMock.Verify(
            client => client.SendMailAsync(It.Is<MailMessage>(
                m => m.Subject == "Aldi Retouren" && m.Body == "Es wurden neue Retouren im Aldi Portal abgerufen."
            )), Times.Once);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendReturnNotificationEmailAsync_LogsErrorOnException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _smtpClientWrapperMock.Setup(client => client.SendMailAsync(It.IsAny<MailMessage>()))
            .ThrowsAsync(exception);

        // Act
        await _emailService.SendReturnNotificationEmailAsync();

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}