using Shared.Entities;

namespace Shared.Contracts;

public interface IOrderRepository
{
    Task CreateOrderAsync(Order order);
    Task<bool> UpdateOrderStatusAsync(string orderCode, string newStatus);
    Task<Order?> GetOrderByOrderCodeAsync(string orderCode);
}