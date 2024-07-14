using Shared.Entities;

namespace Shared.Contracts;

public interface IAccessTokenService
{
    Task EnsureTokenDataExists();
    Task<string> ValidateAndGetAccessToken();
    Task<string> GetAndUpdateNewAccessToken();}