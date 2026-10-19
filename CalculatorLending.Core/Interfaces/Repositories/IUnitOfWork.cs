namespace CalculatorLending.Core.Interfaces.Repositories;

public interface IUnitOfWork
{
    // Throws ConcurrencyConflictException instead of DbUpdateConcurrencyException
    Task<int> SaveChangesAsync(CancellationToken ct);
}
