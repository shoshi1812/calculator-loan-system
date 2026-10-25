using AutoMapper;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Items;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Service.Mapping;
using CalculatorLending.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CalculatorLending.Tests.Unit;

public class ItemServiceTests
{
    private const int ItemId = 10;
    private const string ModelNumber = "FX-991EX";

    private readonly Mock<IItemRepository> _items = new();
    private readonly Mock<IItemCopyRepository> _copies = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private ItemService CreateService() => new(
        _items.Object,
        _copies.Object,
        _categories.Object,
        _unitOfWork.Object,
        _mapper,
        NullLogger<ItemService>.Instance);

    private static ItemCreateRequest CreateRequest(params int[] categoryIds) => new()
    {
        Name = "Casio FX-991EX",
        ModelNumber = ModelNumber,
        CategoryIds = categoryIds.ToList()
    };

    [Fact]
    public async Task CreateAsync_UnknownCategoryId_ReturnsValidation()
    {
        _items.Setup(i => i.ModelNumberExistsAsync(ModelNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _categories.Setup(c => c.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { new() { Id = 1 } });
        var service = CreateService();

        var result = await service.CreateAsync(CreateRequest(1, 2), CancellationToken.None);

        Assert.Equal(ResultStatus.Validation, result.Status);
        _items.Verify(i => i.Add(It.IsAny<Item>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateModelNumber_ReturnsConflict()
    {
        _items.Setup(i => i.ModelNumberExistsAsync(ModelNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService();

        var result = await service.CreateAsync(CreateRequest(1), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _items.Verify(i => i.Add(It.IsAny<Item>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ItemWithLoans_ReturnsConflict()
    {
        _items.Setup(i => i.GetByIdAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Item { Id = ItemId, Name = "Casio FX-991EX", ModelNumber = ModelNumber });
        _items.Setup(i => i.HasLoansAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService();

        var result = await service.DeleteAsync(ItemId, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _items.Verify(i => i.Remove(It.IsAny<Item>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}