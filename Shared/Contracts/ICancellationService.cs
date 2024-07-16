using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface ICancellationService
{
    Task ProcessCancellationEntry(Order order, OrderEntry orderEntry, OrderCancellationEntry cancellationEntry);
    Task CancelWholeOrder(Order order);
    bool AreAllOrderEntriesCancelled(Order order);

}