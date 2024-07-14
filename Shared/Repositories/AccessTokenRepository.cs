using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;

namespace Shared.Repositories;

public class AccessTokenRepository : IAccessTokenRepository
{
    private readonly ApplicationDbContext _context;

    public AccessTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccessToken?> GetFirstTokenAsync()
    {
        try
        {
            return await _context.AccessToken.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Ein unerwarteter Fehler ist aufgetreten beim abrufen des AccessToken.",
                ex);
        }
    }

    public async Task AddTokenAsync(AccessToken token)
    {
        try
        {
            _context.AccessToken.Add(token);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim hinzufügen eines AccessToken.", ex);
        }
    }

    public async Task UpdateTokenAsync(AccessToken token)
    {
        try
        {
            _context.AccessToken.Update(token);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException(
                $"Ein unerwarteter Fehler ist aufgetreten beim aktualisieren des AccessToken.", ex);
        }
    }
}