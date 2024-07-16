using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IReturnRepository
{
    Task<bool> CreateReturnAsync(Return returnObj);
    Task<List<Return>> SearchReturnsAsync(SearchTerm searchTerm, string status);
}