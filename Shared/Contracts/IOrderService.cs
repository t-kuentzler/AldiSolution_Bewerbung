using Shared.Entities;

namespace Shared.Contracts;

public interface IOrderService
{
    Task ProcessSingleOrderAsync(Order order);
    Task<Order> GetOrderByOrderCodeAsync(string orderCode);
}