namespace Shared.Contracts;

public interface IShippedOrdersProcessingService
{
    Task CheckAndProcessShippedOrders();
}