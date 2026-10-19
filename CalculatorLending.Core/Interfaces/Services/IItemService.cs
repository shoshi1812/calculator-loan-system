using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Copies;
using CalculatorLending.Core.DTOs.Items;

namespace CalculatorLending.Core.Interfaces.Services;

public interface IItemService
{
    Task<ServiceResult<PagedResult<ItemResponse>>> GetPagedAsync(ItemQueryParameters q, CancellationToken ct);
    Task<ServiceResult<ItemResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<ItemResponse>> CreateAsync(ItemCreateRequest r, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(int id, ItemUpdateRequest r, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<CopyResponse>>> GetCopiesAsync(int itemId, CancellationToken ct);
    Task<ServiceResult<CopyResponse>> AddCopyAsync(int itemId, CopyCreateRequest r, CancellationToken ct);
    Task<ServiceResult> DeleteCopyAsync(int copyId, CancellationToken ct);
}
