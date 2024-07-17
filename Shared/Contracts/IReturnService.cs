using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IReturnService
{
    List<Return> ParseReturnResponseToReturnObject(ReturnResponse returnResponse);
    Task<bool> CreateReturnAsync(Return returnObj);
    ReturnInProgressRequest? ParseReturnToReturnInProgressRequest(Return returnObj);

    Task<ReturnProcessingResult> ProcessReturn(int orderId,
        Dictionary<int, ReturnEntryModel> returnEntries);

    Task ProcessManualReturnAsync(int orderId, ManualReturnResponse manualReturnResponse,
        Dictionary<int, ReturnEntryModel> returnEntries);

    Task<List<Return>> SearchReturnsAsync(SearchTerm searchTerm, List<string> statuses);
    Task<List<Return>> GetAllReturnsByStatusesAsync(List<string> statuses);
    Task<Return?> GetReturnByIdAsync(int returnId);
    List<ShipmentInfo?> CreateShipmentInfos(Return returnObj);
    Task ProcessShipmentInfoCreation(ShipmentInfoAndReturnIdRequest request);
    Task UpdateReturnConsignmentAndPackagesStatusAsync(string consignmentCode, string status);
    Task UpdateReturnPackagesReceiptDeliveryAsync(string consignmentCode);
    Task<bool> CheckIfAllConsignmentsAreReceived(int returnId);
    Task UpdateReturnStatusAsync(int returnId, string status);
    Task<bool> ProcessPackageStatusUpdateAsync(PackageStatusUpdateRequest request);
}