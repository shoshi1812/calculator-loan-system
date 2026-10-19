using CalculatorLending.Core.Entities;

namespace CalculatorLending.Core.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct); // tracked
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    void Add(Category category);
}
