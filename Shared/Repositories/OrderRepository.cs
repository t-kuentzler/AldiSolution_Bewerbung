using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

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
        catch (OrderNotFoundException)
        {
            throw;
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

    public async Task UpdateOrderStatusByIdAsync(int orderId, string status)
    {
        try
        {
            var order = await _applicationDbContext.Order.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = status;
                _applicationDbContext.Order.Update(order);
                await _applicationDbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren des Status für die Order mit der Id '{orderId}'.",
                ex);
        }
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        try
        {
            return await _applicationDbContext.Order
                .Include(o => o.Consignments)
                .ThenInclude(consignment => consignment.ConsignmentEntries)
                .Include(o => o.Consignments)
                .ThenInclude(c => c.ShippingAddress)
                .Include(o => o.Entries)
                .ThenInclude(entry => entry.DeliveryAddress)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. OrderId: '{orderId}'", ex);
        }
    }

    public async Task<List<Order>> SearchOrdersAsync(SearchTerm searchTerm, string status)
    {
        try
        {
            return await _applicationDbContext.Order
                .Include(o => o.Entries)
                .ThenInclude(entry => entry.DeliveryAddress)
                .Where(o => o.Status == status && (o.Code.Contains(searchTerm.value) ||
                                                   o.Consignments.Any(c => c.TrackingId == searchTerm.value)))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim Suchen von Bestellungen. Suchbegriff: '{searchTerm.value}', Status: '{status}'",
                ex);
        }
    }

    public async Task UpdateOrderEntryAsync(OrderEntry orderEntry)
    {
        try
        {
            _applicationDbContext.Entry(orderEntry).State = EntityState.Modified;
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. OrderEntry: '{orderEntry}'",
                ex);
        }
    }

    public async Task UpdateOrderAsync(Order order)
    {
        try
        {
            _applicationDbContext.Entry(order).State = EntityState.Modified;
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. Order: '{order}'", ex);
        }
    }
}