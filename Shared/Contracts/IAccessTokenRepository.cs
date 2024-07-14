using Shared.Entities;

namespace Shared.Contracts;

public interface IAccessTokenRepository
{
    Task<AccessToken?> GetFirstTokenAsync();
    Task AddTokenAsync(AccessToken token);
    Task UpdateTokenAsync(AccessToken token);
}