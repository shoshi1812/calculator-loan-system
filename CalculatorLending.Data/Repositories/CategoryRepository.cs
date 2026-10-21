using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CalculatorLending.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        // Tracked: attached to an Item when it is created or updated
        var idList = ids.ToList();
        return await _context.Categories
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(ct);
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct)
    {
        return await _context.Categories.AnyAsync(c => c.Name == name, ct);
    }

    public void Add(Category category)
    {
        _context.Categories.Add(category);
    }
}
