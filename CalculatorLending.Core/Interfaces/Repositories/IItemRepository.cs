using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Items;
using CalculatorLending.Core.Entities;

namespace CalculatorLending.Core.Interfaces.Repositories;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, CancellationToken ct); // tracked, with Categories, for update
    Task<ItemWithAvailability?> GetWithAvailabilityAsync(int id, CancellationToken ct); // read-only, no tracking
    Task<PagedResult<ItemWithAvailability>> GetPagedAsync(ItemQueryParameters q, CancellationToken ct);
    Task<bool> ModelNumberExistsAsync(string modelNumber, int? excludeId, CancellationToken ct);
    Task<bool> HasLoansAsync(int itemId, CancellationToken ct);
    void Add(Item item);
    void Remove(Item item);
}
