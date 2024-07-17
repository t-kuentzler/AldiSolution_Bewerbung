using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IReturnRepository
{
    Task<bool> CreateReturnAsync(Return returnObj);
    Task<List<Return>> SearchReturnsAsync(SearchTerm searchTerm, string status);
    Task<List<Return>> GetReturnsWithStatusAsync(string status);
    Task<Return?> GetReturnByIdAsync(int returnId);
    Task UpdateReturnPackageStatusAsync(string packageStatus, int packageId);

    Task UpdateReturnConsignmentStatusQuantityAsync(string packageStatus, int packageQuantity,
        string requestConsignmentCode);
}