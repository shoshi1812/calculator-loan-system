using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Categories;

namespace CalculatorLending.Core.Interfaces.Services;

public interface ICategoryService
{
    Task<ServiceResult<IReadOnlyList<CategoryResponse>>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<CategoryResponse>> CreateAsync(CategoryCreateRequest r, CancellationToken ct);
}
