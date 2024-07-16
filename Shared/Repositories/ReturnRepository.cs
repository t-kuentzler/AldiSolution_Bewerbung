using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Repositories;

public class ReturnRepository : IReturnRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public ReturnRepository(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }
    
    public async Task<bool> CreateReturnAsync(Return returnObj)
    {
        try
        {
            _applicationDbContext.Return.Add(returnObj);
            await _applicationDbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim Erstellen der Retoure. Rma code: '{returnObj.Rma}', OrderCode: '{returnObj.OrderCode}', AldiReturnCode: '{returnObj.AldiReturnCode}'.", ex);
        }
    }
    
    public async Task<List<Return>> SearchReturnsAsync(SearchTerm searchTerm, string status)
    {
        try
        {
            return await _applicationDbContext.Return
                .Include(r => r.CustomerInfo)
                .ThenInclude(entry => entry.Address)
                .Include(r => r.ReturnEntries)
                .ThenInclude(entry => entry.ReturnConsignments)
                .ThenInclude(c => c.Packages)
                .Where(r => r.Status == status
                            && (r.OrderCode.Contains(searchTerm.value)
                                || r.Rma.Contains(searchTerm.value))).ToListAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim Suchen von Retouren. Suchbegriff: '{searchTerm.value}', Status: '{status}'",
                ex);
        }
    }
}