using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IOAuthClientService
{
    Task<OAuthTokenResponse?> GetApiTokenAsync();
    Task<OrderResponse> GetApiOrdersAsync();
    Task<bool> CancelOrderEntriesAsync(string orderCode, IEnumerable<OrderCancellationEntry> cancellationEntries);
    Task<(bool, ManualReturnResponse)> CreateManualReturnAsync(ManualReturnRequest manualReturnRequest);

    Task<(bool, ReceivingReturnResponse)> CreateReceivingReturn(ReceivingReturnRequest parsedReceivingReturnRequest);
    Task<bool> CancelConsignmentAfterDispatchAsync(Consignment consignment);
    Task<bool> ReportReturnPackage(ReportReturnPackageRequest reportReturnPackageRequest);
    Task<bool> UpdateApiOrderStatusInProgressAsync(Order? order, int retryCount = 0);

    Task<ConsignmentListResponse> CreateApiConsignmentAsync(
        List<ConsignmentRequest> consignmentRequestsList, string orderCode, int retryCount = 0);

    Task<ReturnResponse> GetApiReturnsWithStatusCreatedAsync(string status);
    Task<bool> ReturnInProgress(ReturnInProgressRequest returnInProgress, int retryCount = 0);
    Task<bool> ReportConsignmentDeliveryAsync(Consignment consignment, int retryCount = 0);
}