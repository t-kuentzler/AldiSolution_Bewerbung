using Shared.Entities;

namespace Shared.Contracts;

public interface IReturnRepository
{
    Task<bool> CreateReturnAsync(Return returnObj);
}