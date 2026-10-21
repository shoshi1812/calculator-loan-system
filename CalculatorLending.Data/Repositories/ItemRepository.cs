using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Items;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CalculatorLending.Data.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _context;

    public ItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Item?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Items
            .Include(i => i.Categories)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<ItemWithAvailability?> GetWithAvailabilityAsync(int id, CancellationToken ct)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Include(i => i.Categories)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item == null)
        {
            return null;
        }

        var available = await _context.ItemCopies
            .CountAsync(c => c.ItemId == id && c.Status == CopyStatus.Available, ct);

        return new ItemWithAvailability(item, available);
    }

    public async Task<PagedResult<ItemWithAvailability>> GetPagedAsync(ItemQueryParameters q, CancellationToken ct)
    {
        var query = _context.Items.AsNoTracking();

        if (q.CategoryId is int categoryId)
        {
            query = query.Where(i => i.Categories.Any(c => c.Id == categoryId));
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = $"%{q.Search.Trim()}%";
            query = query.Where(i => EF.Functions.ILike(i.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);

        // Id as a tie-breaker keeps the order stable across pages
        var ordered = q.SortDescending
            ? query.OrderByDescending(i => i.Name).ThenByDescending(i => i.Id)
            : query.OrderBy(i => i.Name).ThenBy(i => i.Id);

        var items = await ordered
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Include(i => i.Categories)
            .ToListAsync(ct);

        // One query for the available copies of every item on the page (no N+1)
        var ids = items.Select(i => i.Id).ToList();
        var availableByItem = await _context.ItemCopies
            .Where(c => ids.Contains(c.ItemId) && c.Status == CopyStatus.Available)
            .GroupBy(c => c.ItemId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return new PagedResult<ItemWithAvailability>
        {
            Items = items
                .Select(i => new ItemWithAvailability(i, availableByItem.GetValueOrDefault(i.Id)))
                .ToList(),
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> ModelNumberExistsAsync(string modelNumber, int? excludeId, CancellationToken ct)
    {
        return await _context.Items
            .AnyAsync(i => i.ModelNumber == modelNumber && (excludeId == null || i.Id != excludeId), ct);
    }

    public async Task<bool> HasLoansAsync(int itemId, CancellationToken ct)
    {
        return await _context.Loans.AnyAsync(l => l.ItemCopy.ItemId == itemId, ct);
    }

    public void Add(Item item)
    {
        _context.Items.Add(item);
    }

    public void Remove(Item item)
    {
        _context.Items.Remove(item);
    }
}
