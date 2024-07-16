using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;

namespace Shared.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public OrderRepository(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        try
        {
            _applicationDbContext.Order.Add(order);
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. OrderId: '{order.Id}'.", ex);
        }
    }
    
    public async Task<bool> UpdateOrderStatusAsync(string orderCode, string newStatus)
    {
        try
        {
            var order = await _applicationDbContext.Order.FirstOrDefaultAsync(o => o.Code == orderCode);
            if (order != null)
            {
                order.Status = newStatus;
                order.Modified = DateTime.UtcNow;
                await _applicationDbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. OrderCode: '{orderCode}'.",
                ex);
        }
    }
    
    public async Task<Order?> GetOrderByOrderCodeAsync(string orderCode)
    {
        try
        {
            var order = await _applicationDbContext.Order
                .Include(o => o.Entries)
                .ThenInclude(oe => oe.DeliveryAddress)
                .Include(c => c.Consignments)
                .ThenInclude(ce => ce.ConsignmentEntries)
                .Where(o => o.Code == orderCode)
                .FirstOrDefaultAsync();
                
            return order;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. OrderCode: '{orderCode}'.",
                ex);
        }
    }
    
    public async Task<string?> GetOrderStatusByOrderCodeAsync(string orderCode)
    {
        try
        {
            var order = await _applicationDbContext.Order
                .FirstOrDefaultAsync(o => o.Code == orderCode);
            if (order == null)
            {
                throw new OrderNotFoundException(
                    $"Es wurde keine Order mit dem OrderCode '{orderCode}' in der Datenbank gefunden.");
            }

            return order.Status;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. OrderCode: '{orderCode}'.",
                ex);
        }
    }
    
    public async Task UpdateOrderStatusByOrderCodeAsync(string orderCode, string newStatus)
    {
        try
        {
            var order = await _applicationDbContext.Order.FirstOrDefaultAsync(o => o.Code == orderCode);
            if (order != null)
            {
                order.Status = newStatus;
                _applicationDbContext.Order.Update(order);
                await _applicationDbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. OrderCode: '{orderCode}'.",
                ex);
        }
    }
    
    public async Task<List<Order>> GetOrdersWithStatusAsync(string status)
    {
        try
        {
            return await _applicationDbContext.Order
                .Include(o => o.Entries)
                .ThenInclude(entry => entry.DeliveryAddress)
                .Include(o => o.Consignments) // Consignments mit einschließen
                .Where(o => o.Status == status)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. Status: '{status}'.", ex);
        }
    }
}