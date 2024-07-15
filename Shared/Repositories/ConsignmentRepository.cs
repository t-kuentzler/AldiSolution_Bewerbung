using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;

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
            var consignment = await _applicationDbContext.Consignment.FindAsync(consignmentId);
        
            return consignment;
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist beim Abrufen des Consignment mit der ID '{consignmentId}' aufgetreten.",
                ex);
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
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. Consignment: '{consignment}'.", ex);
        }
    }
}