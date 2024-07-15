using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;

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
}