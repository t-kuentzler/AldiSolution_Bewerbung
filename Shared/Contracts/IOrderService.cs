using Shared.Entities;

namespace Shared.Contracts;

public interface IOrderService
{
    Task ProcessOpenOrdersAsync();
    Task<Order> GetOrderByOrderCodeAsync(string orderCode);
}