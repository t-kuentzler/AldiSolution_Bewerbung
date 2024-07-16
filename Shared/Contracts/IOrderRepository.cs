using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IOrderRepository
{
    Task CreateOrderAsync(Order order);
    Task<bool> UpdateOrderStatusAsync(string orderCode, string newStatus);
    Task<Order?> GetOrderByOrderCodeAsync(string orderCode);
    Task<string?> GetOrderStatusByOrderCodeAsync(string orderCode);
    Task UpdateOrderStatusByOrderCodeAsync(string orderCode, string newStatus);
    Task<List<Order>> GetOrdersWithStatusAsync(string status);
    Task UpdateOrderStatusByIdAsync(int orderId, string status);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<List<Order>> SearchOrdersAsync(SearchTerm searchTerm, string status);
    Task UpdateOrderEntryAsync(OrderEntry orderEntry);
    Task UpdateOrderAsync(Order order);
}