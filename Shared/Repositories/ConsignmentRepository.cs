using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Repositories;

public class ConsignmentRepository : IConsignmentRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public ConsignmentRepository(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public async Task<(bool success, int consignmentId)> SaveConsignmentAsync(Consignment consignment)
    {
        try
        {
            _applicationDbContext.Consignment.Add(consignment);
            await _applicationDbContext.SaveChangesAsync();

            return (true, consignment.Id);
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten. ConsignmentId: '{consignment.Id}'.",
                ex);
        }
    }


    public async Task<Consignment?> GetConsignmentByIdAsync(int consignmentId)
    {
        try
        {
            return await _applicationDbContext.Consignment
                .Include(c => c.ShippingAddress)
                // .Include(c => c.Order)
                .Include(c => c.ConsignmentEntries)
                .ThenInclude(ce => ce.OrderEntry) 
                .ThenInclude(da => da.DeliveryAddress)
                .FirstOrDefaultAsync(c => c.Id == consignmentId);
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten. ConsignmentId: '{consignmentId}'.", ex);
        }
    }


    public async Task SaveConsignmentEntryAsync(ConsignmentEntry consignmentEntry)
    {
        try
        {
            _applicationDbContext.ConsignmentEntry.Add(consignmentEntry);
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten. ConsignmentEntryId: '{consignmentEntry.Id}'.",
                ex);
        }
    }

    public async Task UpdateConsignmentEntryAsync(ConsignmentEntry consignmentEntry)
    {
        try
        {
            _applicationDbContext.Entry(consignmentEntry).State = EntityState.Modified;
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten. ConsignmentEntryId: '{consignmentEntry.Id}'.",
                ex);
        }
    }

    public async Task UpdateConsignmentAsync(Consignment consignment)
    {
        try
        {
            _applicationDbContext.Entry(consignment).State = EntityState.Modified;
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. Consignment: '{consignment}'.",
                ex);
        }
    }

    public async Task<List<Consignment>> GetConsignmentsWithStatusShippedAsync()
    {
        try
        {
            var shippedConsignments = await _applicationDbContext.Consignment
                .Where(c => c.Status == SharedStatus.Shipped && c.Carrier == SharedStatus.Dhl)
                .Include(c => c.ConsignmentEntries)
                .Include(c => c.ShippingAddress)
                .ToListAsync();

            return shippedConsignments;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten.", ex);
        }
    }

    public async Task<bool> UpdateConsignmentStatusByIdAsync(int consignmentId, string newStatus)
    {
        try
        {
            var consignment = await _applicationDbContext.Consignment.FirstOrDefaultAsync(c => c.Id == consignmentId);

            if (consignment != null)
            {
                consignment.Status = newStatus;

                await _applicationDbContext.SaveChangesAsync();

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. ConsignmentId: '{consignmentId}'.",
                ex);
        }
    }
    
    public async Task<Consignment?> GetConsignmentByConsignmentIdAsync(int consignmentId)
    {
        try
        {
            return await _applicationDbContext.Consignment
                .FirstOrDefaultAsync(c => c.Id == consignmentId);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. ConsignmentId: '{consignmentId}'.",
                ex);
        }
    }
    
    public async Task<Consignment?> GetShippedConsignmentByTrackingIdAsync(string trackingId)
    {
        try
        {
            var consignment = await _applicationDbContext.Consignment
                .Include(c => c.ConsignmentEntries)
                .Include(c => c.ShippingAddress)
                .FirstOrDefaultAsync(c => c.TrackingId == trackingId && c.Status == "SHIPPED");

            return consignment; // Null zurückgeben, wenn kein Eintrag gefunden wird oder der Status nicht "SHIPPED" ist
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim abrufen der Consignments mit der TrackingId '{trackingId}' und dem Status 'SHIPPED'.",
                ex);
        }
    }
    
    public async Task UpdateConsignmentStatusByTrackingIdAsync(string newStatus, string trackingId)
    {
        try
        {
            var consignments = await _applicationDbContext.Consignment.Where(c => c.TrackingId == trackingId).ToListAsync();

            if (!consignments.Any() || string.IsNullOrEmpty(newStatus))
            {
                return;
            }

            foreach (var consignment in consignments)
            {
                consignment.Status = newStatus;
            }

            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren des Status für alle Consignments mit der TrackingId '{trackingId}'.",
                ex);
        }
    }
    
    public async Task<List<Consignment>> SearchShippedConsignmentsAsync(SearchTerm searchTerm)
    {
        try
        {
            return await _applicationDbContext.Consignment
                .Include(c => c.ConsignmentEntries)
                .ThenInclude(ce => ce.OrderEntry)
                .Where(c => c.Status == SharedStatus.Shipped &&
                            (c.OrderCode.Contains(searchTerm.value) || c.TrackingId.Contains(searchTerm.value)))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. SearchTerm: '{searchTerm.value}'.",
                ex);
        }
    }
    
    public async Task<List<Consignment>> GetConsignmentsWithStatusAsync(string status)
    {
        try
        {
            return await _applicationDbContext.Consignment
                // .Include(c => c.Order)
                .Where(c => c.Status == status)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. Status: '{status}'.", ex);
        }
    }
    
    public async Task<ConsignmentEntry?> GetConsignmentEntryByIdAsync(int consignmentEntryId)
    {
        try
        {
            return await _applicationDbContext.ConsignmentEntry
                .Include(c => c.Consignment)
                .Include(c => c.OrderEntry)
                .ThenInclude(ce => ce.DeliveryAddress) 
                .FirstOrDefaultAsync(c => c.Id == consignmentEntryId);
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten. ConsignmentEntryId: '{consignmentEntryId}'.", ex);
        }
    }
}