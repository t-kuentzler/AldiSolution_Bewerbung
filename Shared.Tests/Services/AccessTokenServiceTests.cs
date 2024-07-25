using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Shared.Services;

namespace Shared.Tests.Services;

public class AccessTokenServiceTests
{
    private readonly Mock<IAccessTokenRepository> _accessTokenRepositoryMock;
    private readonly Mock<IOAuthClientServiceFactory> _oAuthClientServiceFactoryMock;
    private readonly Mock<IOAuthClientService> _oAuthClientServiceMock;
    private readonly Mock<ILogger<AccessTokenService>> _loggerMock;
    private readonly Mock<IValidatorWrapper<AccessToken>> _accessTokenValidatorMock;
    private readonly AccessTokenService _accessTokenService;
    

    public AccessTokenServiceTests()
    {
        _accessTokenRepositoryMock = new Mock<IAccessTokenRepository>();
        _oAuthClientServiceMock = new Mock<IOAuthClientService>();
        _loggerMock = new Mock<ILogger<AccessTokenService>>();
        _accessTokenValidatorMock = new Mock<IValidatorWrapper<AccessToken>>();
        _oAuthClientServiceFactoryMock = new Mock<IOAuthClientServiceFactory>();

        _oAuthClientServiceFactoryMock.Setup(factory => factory.Create()).Returns(_oAuthClientServiceMock.Object);

        _accessTokenService = new AccessTokenService(
            _accessTokenRepositoryMock.Object, 
            _oAuthClientServiceFactoryMock.Object,
            _loggerMock.Object, 
            _accessTokenValidatorMock.Object);
    }
    
    [Fact]
    public async Task EnsureTokenDataExists_TokenExists_DoesNotCreateToken()
    {
        // Arrange
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ReturnsAsync(new AccessToken());

        // Act
        await _accessTokenService.EnsureTokenDataExists();

        // Assert
        _accessTokenRepositoryMock.Verify(x => x.AddTokenAsync(It.IsAny<AccessToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureTokenDataExists_TokenMissing_CreatesToken()
    {
        // Arrange
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ReturnsAsync((AccessToken?)null);
        _accessTokenValidatorMock.Setup(x => x.ValidateAndThrowAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        _accessTokenRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);

        // Act
        await _accessTokenService.EnsureTokenDataExists();

        // Assert
        _accessTokenRepositoryMock.Verify(x => x.AddTokenAsync(It.IsAny<AccessToken>()), Times.Once);
    }
    
    [Fact]
    public async Task EnsureTokenDataExists_LogsError_WhenAddTokenFails()
    {
        // Arrange
        var exception = new Exception("Datenbankfehler beim Hinzufügen des Tokens");
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ReturnsAsync((AccessToken?)null);
        _accessTokenValidatorMock.Setup(x => x.ValidateAndThrowAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        _accessTokenRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<AccessToken>())).ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<AccessTokenServiceException>(() => _accessTokenService.EnsureTokenDataExists());

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureTokenDataExists_ThrowsAccessTokenServiceException_OnError()
    {
        // Arrange
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<AccessTokenServiceException>(() => _accessTokenService.EnsureTokenDataExists());
        
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
    
    [Fact]
    public async Task EnsureTokenDataExists_ThrowsRepositoryException_OnError()
    {
        // Arrange
        var expectedException = new RepositoryException("Datenbankfehler");
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ThrowsAsync(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _accessTokenService.EnsureTokenDataExists());
        
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
    
    [Fact]
    public async Task EnsureTokenDataExists_ThrowsValidationException_OnValidationFailure()
    {
        // Arrange
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ReturnsAsync((AccessToken?)null);
        _accessTokenValidatorMock
            .Setup(x => x.ValidateAndThrowAsync(It.IsAny<AccessToken>()))
            .ThrowsAsync(new ValidationException("Invalid token"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _accessTokenService.EnsureTokenDataExists());

        Assert.Equal("Invalid token", exception.Message);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }


    [Fact]
    public async Task EnsureTokenDataExists_CreatesToken_WhenMissing()
    {
        // Arrange
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ReturnsAsync((AccessToken?)null);
        _accessTokenValidatorMock.Setup(x => x.ValidateAndThrowAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        _accessTokenRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);

        // Act
        await _accessTokenService.EnsureTokenDataExists();

        // Assert
        _accessTokenRepositoryMock.Verify(x => x.AddTokenAsync(It.IsAny<AccessToken>()), Times.Once, "A new token should be created and saved when none is found.");
    }

    [Fact]
    public async Task EnsureTokenDataExists_DoesNothing_WhenTokenExists()
    {
        // Arrange
        var existingToken = new AccessToken { Token = "ExistingToken", ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _accessTokenRepositoryMock.Setup(x => x.GetFirstTokenAsync()).ReturnsAsync(existingToken);

        // Act
        await _accessTokenService.EnsureTokenDataExists();

        // Assert
        _accessTokenRepositoryMock.Verify(x => x.AddTokenAsync(It.IsAny<AccessToken>()), Times.Never, "No new token should be created when one already exists.");
    }
    
    //ValidateAndGetAccessToken
    [Fact]
    public async Task ValidateAndGetAccessToken_ReturnsExistingValidToken()
    {
        //Arrange
        var validToken = new AccessToken
        {
            Token = "ValidToken",
            ExpiresAt = DateTime.Now.AddMinutes(20) // Stellt sicher, dass der Token gültig ist
        };

        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync()).ReturnsAsync(validToken);

        //Act
        var result = await _accessTokenService.ValidateAndGetAccessToken();

        //Assert
        Assert.Equal("ValidToken", result);
        _accessTokenRepositoryMock.Verify(repo => repo.GetFirstTokenAsync(), Times.Once);
        
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAndGetAccessToken_UpdatesExistingTokenInDb_WhenTokenIsInvalidAndTokenExistsInDb()
    {
        var invalidToken = new AccessToken
        {
            Token = "NewInvalidToken",
            ExpiresAt = DateTime.UtcNow
        };

        var newToken = new OAuthTokenResponse
        {
            AccessToken = "NewValidToken",
            ExpiresIn = 19954
        };
        
        //Arrange
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync())
            .ReturnsAsync(invalidToken); 
    
        _accessTokenValidatorMock.Setup(validator => validator.ValidateAndThrowAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        _accessTokenRepositoryMock.Setup(repo => repo.AddTokenAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync()).ReturnsAsync(newToken);
        
        //Act
        await _accessTokenService.ValidateAndGetAccessToken();
    
        //Assert
        _accessTokenRepositoryMock.Verify(repo => repo.UpdateTokenAsync(It.IsAny<AccessToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateAndGetAccessToken_CreatesNewTokenInDb_WhenTokenIsInvalidAndTokenDoesntExistsInDb()
    {
        //Arrange
        var newToken = new OAuthTokenResponse
        {
            AccessToken = "NewValidToken",
            ExpiresIn = 19954
        };
        
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync())
            .ReturnsAsync((AccessToken?)null); 
    
        _accessTokenValidatorMock.Setup(validator => validator.ValidateAndThrowAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        _accessTokenRepositoryMock.Setup(repo => repo.AddTokenAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync()).ReturnsAsync(newToken);
        
        //Act
        await _accessTokenService.ValidateAndGetAccessToken();
    
        //Assert
        _accessTokenRepositoryMock.Verify(repo => repo.AddTokenAsync(It.IsAny<AccessToken>()), Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task ValidateAndGetAccessToken_ReturnsToken_WhenTokenIsValidInDb()
    {
        //Arrange
        var token = new AccessToken
        {
            Token = "ValidDbToken",
            ExpiresAt = DateTime.Now.AddMinutes(20)
        };
        
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync())
            .ReturnsAsync(token);
        _accessTokenValidatorMock.Setup(validator => validator.ValidateAndThrowAsync(It.IsAny<AccessToken>())).Returns(Task.CompletedTask);
        
        //Act
        var result = await _accessTokenService.ValidateAndGetAccessToken();
    
        //Assert
        Assert.Equal(token.Token, result);
    }

    //GetAndUpdateNewAccessToken
    [Fact]
    public async Task GetAndUpdateNewAccessToken_UpdatesToken_WhenTokenIsNotNullAndTokenExistsInDb()
    {
        // Arrange
        var tokenResponse = new OAuthTokenResponse
        {
            AccessToken = "ValidToken",
            ExpiresIn = 1111111
        };

        var token = new AccessToken
        {
            Token = "TokenFromDb",
            ExpiresAt = DateTime.UtcNow
        };

        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync())
            .ReturnsAsync(tokenResponse);

        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync())
            .ReturnsAsync(token);
        
        // Act
        var newToken = await _accessTokenService.GetAndUpdateNewAccessToken();
        
        // Assert
        Assert.NotNull(newToken);
        _accessTokenRepositoryMock.Verify(repo => repo.UpdateTokenAsync(It.IsAny<AccessToken>()), Times.Once);
    }


    [Fact]
    public async Task GetAndUpdateNewAccessToken_CreatesToken_WhenTokenIsNotNullAndTokenDoesntExistsInDb()
    {
        //Arrange
        var tokenResponse = new OAuthTokenResponse
        {
            AccessToken = "ValidToken",
            ExpiresIn = 1111111
        };
        
    _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync())
            .ReturnsAsync(tokenResponse);

    _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync())
        .ReturnsAsync((AccessToken?)null);
        
        //Act
        var newToken = await _accessTokenService.GetAndUpdateNewAccessToken();
        
        //Assert
        Assert.NotNull(newToken);
        _accessTokenRepositoryMock.Verify(repo => repo.AddTokenAsync(It.IsAny<AccessToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAndUpdateNewAccessToken_ThrowsAccessTokenIsNullExceptionAndLogs_WhenAccessTokenIsNull()
    {
        //Arrange
        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync())
            .ReturnsAsync((OAuthTokenResponse?)null);
        
        //Act & Assert
        await Assert.ThrowsAsync<AccessTokenIsNullException>(() => _accessTokenService.GetAndUpdateNewAccessToken());
        
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
    
    
    [Fact]
    public async Task UpdateExistingToken_UpdatesExistingTokenSuccessfully()
    {
        // Arrange
        var newTokenValue = "NewValidToken";
        var oldToken = new AccessToken
        {
            Token = "OldToken",
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Simulate expired token
        };

        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync())
            .ReturnsAsync(new OAuthTokenResponse { AccessToken = newTokenValue, ExpiresIn = 3600 });
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync()).ReturnsAsync(oldToken);
        _accessTokenRepositoryMock.Setup(repo => repo.UpdateTokenAsync(It.Is<AccessToken>(t => t.Token == newTokenValue && t.ExpiresAt > DateTime.UtcNow)))
            .Returns(Task.CompletedTask);

        // Act
        var resultToken = await _accessTokenService.GetAndUpdateNewAccessToken();

        // Assert
        Assert.Equal(newTokenValue, resultToken);
        _accessTokenRepositoryMock.Verify(repo => repo.UpdateTokenAsync(It.IsAny<AccessToken>()), Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(2));    
    }

    [Fact]
    public async Task UpdateExistingToken_ThrowsRepositoryException_WhenDbUpdateFails()
    {
        // Arrange
        var tokenResponse = new OAuthTokenResponse { AccessToken = "NewValidToken" };
        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync()).ReturnsAsync(tokenResponse);
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync()).ReturnsAsync(new AccessToken());
        _accessTokenRepositoryMock.Setup(repo => repo.UpdateTokenAsync(It.IsAny<AccessToken>()))
            .Throws(new RepositoryException("Datenbankfehler"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _accessTokenService.GetAndUpdateNewAccessToken());
        _accessTokenRepositoryMock.Verify(repo => repo.UpdateTokenAsync(It.IsAny<AccessToken>()), Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);  
        
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(2));
    }
    
    [Fact]
    public async Task UpdateExistingToken_ThrowsAccessTokenServiceException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var tokenResponse = new OAuthTokenResponse { AccessToken = "NewValidToken" };
        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync()).ReturnsAsync(tokenResponse);
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync()).ReturnsAsync(new AccessToken());
        _accessTokenRepositoryMock.Setup(repo => repo.UpdateTokenAsync(It.IsAny<AccessToken>()))
            .Throws(new Exception("Unerwarteter Fehler"));

        // Act & Assert
        await Assert.ThrowsAsync<AccessTokenServiceException>(() => _accessTokenService.GetAndUpdateNewAccessToken());
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(1));  
    }

    [Fact]
    public async Task CreateNewToken_CreatesNewTokenSuccessfully()
    {
        // Arrange
        var newTokenValue = "NewValidToken";
        AccessToken? token = null;

        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync())
            .ReturnsAsync(new OAuthTokenResponse { AccessToken = newTokenValue, ExpiresIn = 3600 });
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync()).ReturnsAsync(token);
        _accessTokenRepositoryMock.Setup(repo => repo.AddTokenAsync(It.Is<AccessToken>(t => t.Token == newTokenValue && t.ExpiresAt > DateTime.UtcNow)))
            .Returns(Task.CompletedTask);

        // Act
        var resultToken = await _accessTokenService.GetAndUpdateNewAccessToken();

        // Assert
        Assert.Equal(newTokenValue, resultToken);
        _accessTokenRepositoryMock.Verify(repo => repo.AddTokenAsync(It.IsAny<AccessToken>()), Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(2));    
    }

    [Fact]
    public async Task CreateNewToken_ThrowsRepositoryException_WhenDbUpdateFails()
    {
        // Arrange
        AccessToken? token = null;
        var tokenResponse = new OAuthTokenResponse { AccessToken = "NewValidToken" };
        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync()).ReturnsAsync(tokenResponse);
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync()).ReturnsAsync(token);
        _accessTokenRepositoryMock.Setup(repo => repo.AddTokenAsync(It.IsAny<AccessToken>()))
            .Throws(new RepositoryException("Datenbankfehler"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _accessTokenService.GetAndUpdateNewAccessToken());
        _accessTokenRepositoryMock.Verify(repo => repo.AddTokenAsync(It.IsAny<AccessToken>()), Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(1));  
    }
    
    [Fact]
    public async Task CreateNewToken_ThrowsAccessTokenServiceException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        AccessToken? token = null;
        var tokenResponse = new OAuthTokenResponse { AccessToken = "NewValidToken" };
        _oAuthClientServiceMock.Setup(service => service.GetApiTokenAsync()).ReturnsAsync(tokenResponse);
        _accessTokenRepositoryMock.Setup(repo => repo.GetFirstTokenAsync()).ReturnsAsync(token);
        _accessTokenRepositoryMock.Setup(repo => repo.AddTokenAsync(It.IsAny<AccessToken>()))
            .Throws(new Exception("Unerwarteter Fehler"));

        // Act & Assert
        await Assert.ThrowsAsync<AccessTokenServiceException>(() => _accessTokenService.GetAndUpdateNewAccessToken());
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(1));  
    }


}