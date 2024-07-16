using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IOrderService
{
    Task ProcessSingleOrderAsync(Order order);
    Task<Order> GetOrderByOrderCodeAsync(string orderCode);
    Task<string> GetOrderStatusByOrderCodeAsync(string orderCode);
    Task<bool> UpdateSingleOrderStatusInDatabaseAsync(string orderCode, string status);
    Task<List<Order>> GetOrdersByStatusAsync(string status);
    Task UpdateOrderStatusByOrderCodeAsync(string orderCode, string newStatus);
    Task UpdateOrderStatusByIdAsync(int orderId, string status);
    Task<List<Order>> GetAllOrdersByStatusAsync(string status);
    Task<Order> GetOrderByIdAsync(int orderId);
    Task<List<Order>> SearchOrdersAsync(SearchTerm searchTerm, string status);

    Task<bool> ProcessOrderEntriesCancellationAsync(int orderId, string orderCode,
        Dictionary<int, CancelOrderEntryModel> cancelledEntries);
}