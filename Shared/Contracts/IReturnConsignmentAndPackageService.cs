using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IReturnConsignmentAndPackageService
{
    Task UpdateAllReturnPackageStatusFromReturnAsync(string packageStatus, Return returnObj);
    Task UpdateReturnConsignmentStatusQuantityAsync(string packageStatus, Return returnObj);
    Task UpdateCompletedDateForAllReturnConsignments(Return returnObj);
    Task UpdateReturnConsignmentAsync(ReturnConsignment returnConsignment);

    Task<Return> CreateReturnConsignmentAndReturnPackage(
        ReceivingReturnResponse parsedReceivingReturnResponse,
        Return returnObj);

    Task<ReturnConsignment> GetReturnConsignmentByConsignmentCodeAsync(string consignmentCode);
}