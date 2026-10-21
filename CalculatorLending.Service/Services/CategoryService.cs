using AutoMapper;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Categories;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CalculatorLending.Service.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CategoryService> logger)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryResponse>>> GetAllAsync(CancellationToken ct)
    {
        var categories = await _categories.GetAllAsync(ct);
        var response = _mapper.Map<List<CategoryResponse>>(categories);

        return ServiceResult<IReadOnlyList<CategoryResponse>>.Success(response);
    }

    public async Task<ServiceResult<CategoryResponse>> CreateAsync(CategoryCreateRequest r, CancellationToken ct)
    {
        var name = r.Name.Trim();

        if (await _categories.NameExistsAsync(name, ct))
        {
            _logger.LogWarning("Category creation rejected: name already exists");
            return ServiceResult<CategoryResponse>.Conflict("Category name already exists");
        }

        var category = _mapper.Map<Category>(r);
        category.Name = name;
        _categories.Add(category);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // Two creations with the same name at the same moment
            _logger.LogWarning("Category creation rejected: duplicate name detected on save");
            return ServiceResult<CategoryResponse>.Conflict("Category name already exists");
        }

        _logger.LogInformation("Category {CategoryId} created", category.Id);
        return ServiceResult<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
    }
}
