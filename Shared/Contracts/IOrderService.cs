using Shared.Entities;

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
}