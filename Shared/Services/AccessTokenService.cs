using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class AccessTokenService : IAccessTokenService
{
    private readonly IAccessTokenRepository _tokenRepository;
    private readonly IOAuthClientServiceFactory _oAuthClientServiceFactory;
    private readonly ILogger<AccessTokenService> _logger;
    private readonly IValidatorWrapper<AccessToken> _tokenValidator;


    public AccessTokenService(IAccessTokenRepository tokenRepository,
        IOAuthClientServiceFactory oAuthClientServiceFactory,
        ILogger<AccessTokenService> logger, IValidatorWrapper<AccessToken> tokenValidator)
    {
        _tokenRepository = tokenRepository;
        _oAuthClientServiceFactory = oAuthClientServiceFactory;
        _logger = logger;
        _tokenValidator = tokenValidator;
    }

    public async Task EnsureTokenDataExists()
    {
        if (await IsTokenMissingAsync())
        {
            await CreateAndSaveInitialTokenAsync();
        }
    }

    private async Task<bool> IsTokenMissingAsync()
    {
        try
        {
            var token = await _tokenRepository.GetFirstTokenAsync();
            return token == null;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, "Repository-Exception beim Abrufen des AccessToken.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unerwarteter Fehler beim Abrufen des AccessToken.");
            throw new AccessTokenServiceException("Unerwarteter Fehler beim Abrufen des AccessToken.", ex);
        }
    }

    private async Task CreateAndSaveInitialTokenAsync()
    {
        var initialToken = new AccessToken
        {
            Token = "InitialToken",
            ExpiresAt = DateTime.Now
        };

        await ValidateAndSaveTokenAsync(initialToken);
    }

    private async Task ValidateAndSaveTokenAsync(AccessToken token)
    {
        try
        {
            await _tokenValidator.ValidateAndThrowAsync(token);
            await _tokenRepository.AddTokenAsync(token);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validierungsfehler bei der Erstellung des Initial-Datensatzes für den AccessToken.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Erstellung des Initial-Datensatzes für den AccessToken.");
            throw new AccessTokenServiceException(
                "Fehler bei der Erstellung des Initial-Datensatzes für den AccessToken.", ex);
        }
    }

    private async Task<(bool IsValid, AccessToken? Token)> IsTokenValid()
    {
        try
        {
            var token = await _tokenRepository.GetFirstTokenAsync();
            if (token != null)
            {
                bool isValid = token.ExpiresAt > DateTime.Now;
                return (isValid, token);
            }

            return (false, null);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, "Repository-Exception beim Überprüfen der Token-Gültigkeit.");
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unerwarteter Fehler beim Überprüfen der Token-Gültigkeit.");
            return (false, null);
        }
    }

    public async Task<string> ValidateAndGetAccessToken() //Prüfung ob Token in Datenbank noch gültig ist
    {
        var (tokenIsValid, token) = await IsTokenValid();

        if (tokenIsValid && token != null)
        {
            _logger.LogInformation("Der AccessToken in der Datenbank ist noch gültig.");
            return token.Token;
        }

        return await GetAndUpdateNewAccessToken();
    }

    public async Task<string> GetAndUpdateNewAccessToken()
    {
        var oAuthClientService = _oAuthClientServiceFactory.Create();

        OAuthTokenResponse? tokenResponse = null;
        try
        {
            tokenResponse = await oAuthClientService.GetApiTokenAsync();
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception("Es ist ein unerwarteter Fehler beim Abrufen des AccessToken von der API aufgetreten.",
                ex);
        }

        if (tokenResponse?.AccessToken == null)
        {
            _logger.LogError("Der empfangene AccessToken der API ist null.");
            throw new AccessTokenIsNullException("Der empfangene AccessToken der API ist null.");
        }

        await UpdateToken(tokenResponse.AccessToken);
        return tokenResponse.AccessToken;
    }

    private async Task UpdateToken(string? newToken)
    {
        if (newToken == null)
        {
            _logger.LogError("Der empfangene AccessToken der API ist null.");
            return;
        }

        try
        {
            var token = await _tokenRepository.GetFirstTokenAsync();
            if (token != null)
            {
                await UpdateExistingToken(token, newToken);
            }
            else
            {
                await CreateNewToken(newToken);
            }
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, "Repository-Exception beim Überprüfen der Token-Gültigkeit.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Es ist ein unerwarteter Fehler beim aktualisieren des AccessToken in der Datenbank aufgetreten.");
            throw new AccessTokenServiceException(
                "Unerwarteter Fehler beim aktualisieren des AccessToken in der Datenbank.", ex);
        }
    }


    private async Task UpdateExistingToken(AccessToken token, string newToken)
    {
        token.Token = newToken;
        token.ExpiresAt = CalculateExpiresAt();

        _logger.LogInformation("Es wird versucht den AccessToken in der Datenbank zu aktualisieren.");

        try
        {
            await _tokenValidator.ValidateAndThrowAsync(token);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(
                $"Es ist ein Fehler bei der Aktualisierung des AccessToken in der Datenbank aufgetreten: {ex.Message}");
            throw;
        }

        try
        {
            await _tokenRepository.UpdateTokenAsync(token);
            _logger.LogInformation("Der AccessToken wurde erfolgreich in der Datenbank aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, "Repository-Exception beim aktualisieren des AccessToken in der Datenbank.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Es ist ein unerwarteter Fehler beim aktualisieren des AccessToken in der Datenbank aufgetreten.");
            throw new AccessTokenServiceException(
                "Es ist ein unerwarteter Fehler beim aktualisieren des AccessToken in der Datenbank aufgetreten.", ex);
        }
    }

    private async Task CreateNewToken(string newToken)
    {
        var newAccessToken = new AccessToken
        {
            Token = newToken,
            ExpiresAt = CalculateExpiresAt()
        };

        _logger.LogInformation(
            "Es wird versucht einen neuen Datensatz für den AccessToken in der Datenbank zu erstellen.");

        try
        {
            await _tokenValidator.ValidateAndThrowAsync(newAccessToken);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(
                $"Es ist ein Fehler bei der Erstellung des AccessToken in der Datenbank aufgetreten: {ex.Message}");
            throw;
        }

        try
        {
            await _tokenRepository.AddTokenAsync(newAccessToken);
            _logger.LogInformation(
                "Es wurde erfolgreich ein neuer Datensatz für den AccessToken in der Datenbank erstellt.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, "Repository-Exception beim erstellen eines neuen AccessToken in der Datenbank.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Es ist ein unerwarteter Fehler beim erstellen des AccessToken in der Datenbank aufgetreten.");
            throw new AccessTokenServiceException(
                "Es ist ein unerwarteter Fehler beim erstellen des AccessToken in der Datenbank aufgetreten.", ex);
        }
    }

    private DateTime CalculateExpiresAt()
    {
        return DateTime.Now.AddSeconds(3000);
    }
}