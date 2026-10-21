using AutoMapper;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Copies;
using CalculatorLending.Core.DTOs.Items;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CalculatorLending.Service.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _items;
    private readonly IItemCopyRepository _copies;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ItemService> _logger;

    public ItemService(
        IItemRepository items,
        IItemCopyRepository copies,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ItemService> logger)
    {
        _items = items;
        _copies = copies;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<ItemResponse>>> GetPagedAsync(ItemQueryParameters q, CancellationToken ct)
    {
        var paged = await _items.GetPagedAsync(q, ct);

        var response = new PagedResult<ItemResponse>
        {
            Items = _mapper.Map<List<ItemResponse>>(paged.Items),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };

        return ServiceResult<PagedResult<ItemResponse>>.Success(response);
    }

    public async Task<ServiceResult<ItemResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var item = await _items.GetWithAvailabilityAsync(id, ct);
        if (item is null)
        {
            return ServiceResult<ItemResponse>.NotFound("Item not found");
        }

        return ServiceResult<ItemResponse>.Success(_mapper.Map<ItemResponse>(item));
    }

    public async Task<ServiceResult<ItemResponse>> CreateAsync(ItemCreateRequest r, CancellationToken ct)
    {
        if (await _items.ModelNumberExistsAsync(r.ModelNumber, null, ct))
        {
            _logger.LogWarning("Item creation rejected: model number already exists");
            return ServiceResult<ItemResponse>.Conflict("Model number already exists");
        }

        var categoryIds = r.CategoryIds.Distinct().ToList();
        var categories = await _categories.GetByIdsAsync(categoryIds, ct);
        if (categories.Count != categoryIds.Count)
        {
            return ServiceResult<ItemResponse>.Validation("One or more categories do not exist");
        }

        var item = _mapper.Map<Item>(r);
        item.Categories = categories.ToList();
        _items.Add(item);

        if (!await TrySaveAsync(ct))
        {
            _logger.LogWarning("Item creation rejected: duplicate model number detected on save");
            return ServiceResult<ItemResponse>.Conflict("Model number already exists");
        }

        _logger.LogInformation("Item {ItemId} created", item.Id);

        // A new item has no copies yet
        var response = _mapper.Map<ItemResponse>(new ItemWithAvailability(item, 0));
        return ServiceResult<ItemResponse>.Success(response);
    }

    public async Task<ServiceResult> UpdateAsync(int id, ItemUpdateRequest r, CancellationToken ct)
    {
        var item = await _items.GetByIdAsync(id, ct);
        if (item is null)
        {
            return ServiceResult.NotFound("Item not found");
        }

        if (await _items.ModelNumberExistsAsync(r.ModelNumber, id, ct))
        {
            _logger.LogWarning("Item {ItemId} update rejected: model number already exists", id);
            return ServiceResult.Conflict("Model number already exists");
        }

        var categoryIds = r.CategoryIds.Distinct().ToList();
        var categories = await _categories.GetByIdsAsync(categoryIds, ct);
        if (categories.Count != categoryIds.Count)
        {
            return ServiceResult.Validation("One or more categories do not exist");
        }

        item.Name = r.Name;
        item.ModelNumber = r.ModelNumber;
        item.Description = r.Description;

        // EF works out which rows to add and remove in the link table
        item.Categories.Clear();
        foreach (var category in categories)
        {
            item.Categories.Add(category);
        }

        if (!await TrySaveAsync(ct))
        {
            _logger.LogWarning("Item {ItemId} update rejected: duplicate model number detected on save", id);
            return ServiceResult.Conflict("Model number already exists");
        }

        _logger.LogInformation("Item {ItemId} updated", id);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await _items.GetByIdAsync(id, ct);
        if (item is null)
        {
            return ServiceResult.NotFound("Item not found");
        }

        if (await _items.HasLoansAsync(id, ct))
        {
            _logger.LogWarning("Item {ItemId} deletion rejected: loans exist", id);
            return ServiceResult.Conflict("Cannot delete: loans exist for this item");
        }

        // The copies are deleted by cascade
        _items.Remove(item);

        if (!await TrySaveAsync(ct))
        {
            _logger.LogWarning("Item {ItemId} deletion rejected: conflict detected on save", id);
            return ServiceResult.Conflict("The item was changed by someone else. Please try again");
        }

        _logger.LogInformation("Item {ItemId} deleted", id);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IReadOnlyList<CopyResponse>>> GetCopiesAsync(int itemId, CancellationToken ct)
    {
        if (await _items.GetWithAvailabilityAsync(itemId, ct) is null)
        {
            return ServiceResult<IReadOnlyList<CopyResponse>>.NotFound("Item not found");
        }

        var copies = await _copies.GetByItemIdAsync(itemId, ct);
        var response = _mapper.Map<List<CopyResponse>>(copies);

        return ServiceResult<IReadOnlyList<CopyResponse>>.Success(response);
    }

    public async Task<ServiceResult<CopyResponse>> AddCopyAsync(int itemId, CopyCreateRequest r, CancellationToken ct)
    {
        if (await _items.GetWithAvailabilityAsync(itemId, ct) is null)
        {
            return ServiceResult<CopyResponse>.NotFound("Item not found");
        }

        if (await _copies.SerialNumberExistsAsync(r.SerialNumber, ct))
        {
            _logger.LogWarning("Copy creation for item {ItemId} rejected: serial number already exists", itemId);
            return ServiceResult<CopyResponse>.Conflict("Serial number already exists");
        }

        var copy = _mapper.Map<ItemCopy>(r);
        copy.ItemId = itemId;
        copy.Status = CopyStatus.Available;
        _copies.Add(copy);

        if (!await TrySaveAsync(ct))
        {
            _logger.LogWarning("Copy creation for item {ItemId} rejected: duplicate serial number detected on save", itemId);
            return ServiceResult<CopyResponse>.Conflict("Serial number already exists");
        }

        _logger.LogInformation("Copy {CopyId} added to item {ItemId}", copy.Id, itemId);
        return ServiceResult<CopyResponse>.Success(_mapper.Map<CopyResponse>(copy));
    }

    public async Task<ServiceResult> DeleteCopyAsync(int copyId, CancellationToken ct)
    {
        var copy = await _copies.GetByIdAsync(copyId, ct);
        if (copy is null)
        {
            return ServiceResult.NotFound("Copy not found");
        }

        if (await _copies.HasLoanAsync(copyId, ct))
        {
            _logger.LogWarning("Copy {CopyId} deletion rejected: loans exist", copyId);
            return ServiceResult.Conflict("Cannot delete: loans exist for this copy");
        }

        _copies.Remove(copy);

        if (!await TrySaveAsync(ct))
        {
            _logger.LogWarning("Copy {CopyId} deletion rejected: conflict detected on save", copyId);
            return ServiceResult.Conflict("The copy was changed by someone else. Please try again");
        }

        _logger.LogInformation("Copy {CopyId} deleted", copyId);
        return ServiceResult.Success();
    }

    // Returns false when the save failed because of a concurrency or uniqueness conflict
    private async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }
        catch (ConcurrencyConflictException)
        {
            return false;
        }
    }
}
