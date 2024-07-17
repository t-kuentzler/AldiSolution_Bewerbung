using Microsoft.EntityFrameworkCore;
using Shared.Constants;
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
    
    public async Task<List<Return>> GetReturnsWithStatusAsync(string status)
    {
        try
        {
            return await _applicationDbContext.Return
                .Include(r => r.CustomerInfo)
                .ThenInclude(entry => entry.Address)
                .Include(r => r.ReturnEntries)
                .ThenInclude(entry => entry.ReturnConsignments)
                .ThenInclude(c => c.Packages)
                .Where(o => o.Status == status)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. Status: '{status}'", ex);
        }
    }
    
    public async Task<Return?> GetReturnByIdAsync(int returnId)
    {
        try
        {
            return await _applicationDbContext.Return
                .Include(r => r.CustomerInfo)
                .ThenInclude(ci => ci.Address)
                .Include(r => r.ReturnEntries)
                .ThenInclude(re => re.ReturnConsignments)
                .ThenInclude(rc => rc.Packages)
                .Include(r => r.Order)
                .ThenInclude(o => o.Entries)
                .FirstOrDefaultAsync(r => r.Id == returnId);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten. Return mit der Id '{returnId}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnPackageStatusAsync(string packageStatus, int packageId)
    {
        try
        {
            var returnPackage =
                await _applicationDbContext.ReturnPackage.FirstOrDefaultAsync(o =>
                    o.Id == packageId);
            if (returnPackage != null)
            {
                returnPackage.Status = packageStatus;

                await _applicationDbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren des Status für das ReturnPackage mit der Id '{packageId}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnConsignmentStatusQuantityAsync(string packageStatus, int packageQuantity,
        string requestConsignmentCode)
    {
        try
        {
            var returnConsignment =
                await _applicationDbContext.ReturnConsignment.FirstOrDefaultAsync(o =>
                    o.ConsignmentCode == requestConsignmentCode);
            if (returnConsignment != null)
            {
                if (packageStatus.Equals(SharedStatus.Canceled))
                {
                    returnConsignment.CanceledQuantity += packageQuantity;
                }
                else if (packageStatus.Equals(SharedStatus.Completed))
                {
                    returnConsignment.CompletedQuantity += packageQuantity;
                }

                await _applicationDbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren der {packageStatus} Quantity für das ReturnConsignment mit dem ConsignmentCode '{requestConsignmentCode}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnConsignmentAsync(ReturnConsignment returnConsignment)
    {
        try
        {
            _applicationDbContext.ReturnConsignment.Update(returnConsignment);
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren der ReturnConsignment mit der Id '{returnConsignment.Id}'.",
                ex);
        }
    }
    
    public async Task<ReturnConsignment?> GetReturnConsignmentByConsignmentCodeAsync(string consignmentCode)
    {
        try
        {
            return await _applicationDbContext.ReturnConsignment
                .FirstOrDefaultAsync(rc => rc.ConsignmentCode == consignmentCode);
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten. ReturnConsignment mit ConsignmentCode '{consignmentCode}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnAsync(Return returnObj)
    {
        try
        {
            _applicationDbContext.Return.Update(returnObj);
            await _applicationDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren der Retoure mit der Id '{returnObj.Id}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnStatusByIdAsync(int returnId, string status)
    {
        try
        {
            var returnObj = await _applicationDbContext.Return.FirstOrDefaultAsync(o => o.Id == returnId);
            if (returnObj != null)
            {
                returnObj.Status = status;
                _applicationDbContext.Return.Update(returnObj);
                await _applicationDbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren des Status für die Return mit der Id '{returnId}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnConsignmentAndPackagesStatusAsync(string consignmentCode, string status)
    {
        try
        {
            var returnConsignment =
                await _applicationDbContext.ReturnConsignment.Include(rc => rc.Packages).FirstOrDefaultAsync(o =>
                    o.ConsignmentCode == consignmentCode);
            if (returnConsignment != null)
            {
                returnConsignment.Status = status;

                foreach (var package in returnConsignment.Packages)
                {
                    package.Status = status;
                }

                await _applicationDbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren des Status für die ReturnConsignment und ReturnPackages mit dem ConsignmentCode '{consignmentCode}'.",
                ex);
        }
    }
}