using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CalculatorLending.Data.Repositories;

public class ItemCopyRepository : IItemCopyRepository
{
    private readonly AppDbContext _context;

    public ItemCopyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ItemCopy?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.ItemCopies.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<ItemCopy>> GetByItemIdAsync(int itemId, CancellationToken ct)
    {
        return await _context.ItemCopies
            .AsNoTracking()
            .Where(c => c.ItemId == itemId)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task<ItemCopy?> GetFirstAvailableAsync(int itemId, CancellationToken ct)
    {
        // Tracked: this is the copy that concurrent loans compete for
        return await _context.ItemCopies
            .Where(c => c.ItemId == itemId && c.Status == CopyStatus.Available)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> SerialNumberExistsAsync(string serial, CancellationToken ct)
    {
        return await _context.ItemCopies.AnyAsync(c => c.SerialNumber == serial, ct);
    }

    public async Task<bool> HasLoanAsync(int copyId, CancellationToken ct)
    {
        return await _context.Loans.AnyAsync(l => l.ItemCopyId == copyId, ct);
    }

    public void Add(ItemCopy copy)
    {
        _context.ItemCopies.Add(copy);
    }

    public void Remove(ItemCopy copy)
    {
        _context.ItemCopies.Remove(copy);
    }
}
