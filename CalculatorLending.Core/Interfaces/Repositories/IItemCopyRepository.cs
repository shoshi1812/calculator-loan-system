using CalculatorLending.Core.Entities;

namespace CalculatorLending.Core.Interfaces.Repositories;

public interface IItemCopyRepository
{
    Task<ItemCopy?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ItemCopy>> GetByItemIdAsync(int itemId, CancellationToken ct); // read-only
    Task<ItemCopy?> GetFirstAvailableAsync(int itemId, CancellationToken ct); // tracked, Status == Available
    Task<bool> SerialNumberExistsAsync(string serial, CancellationToken ct);
    Task<bool> HasLoanAsync(int copyId, CancellationToken ct);
    void Add(ItemCopy copy);
    void Remove(ItemCopy copy);
}
