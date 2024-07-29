using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IOAuthClientService
{
    Task<OrderResponse> GetApiOrdersAsync();
    Task<bool> UpdateApiOrderStatusInProgressAsync(Order? order, int retryCount = 0);
    Task<bool> CancelOrderEntriesAsync(string orderCode, IEnumerable<OrderCancellationEntry> cancellationEntries);

}