using CalculatorLending.Core.Common;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CalculatorLending.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Row version (xmin) changed since it was read
            throw new ConcurrencyConflictException("The data was changed by another request.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Second line of defense: the partial unique index on active loans (stage 2.10)
            throw new ConcurrencyConflictException("The data was changed by another request.", ex);
        }
    }
}
