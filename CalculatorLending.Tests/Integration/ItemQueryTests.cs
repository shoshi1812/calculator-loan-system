using CalculatorLending.Core.DTOs.Items;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Data.Repositories;

namespace CalculatorLending.Tests.Integration;

[Collection("Postgres")]
public class ItemQueryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public ItemQueryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // Five items; Bravo and Delta are Graphing; Charlie has 3 copies, one of them loaned
    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await using var context = _fixture.CreateContext();

        var scientific = new Category { Name = "Scientific" };
        var graphing = new Category { Name = "Graphing" };
        var items = new[]
        {
            new Item { Name = "Alpha Calc", ModelNumber = "A-1", Categories = { scientific } },
            new Item { Name = "Bravo Calc", ModelNumber = "B-1", Categories = { graphing } },
            new Item { Name = "Charlie Calc", ModelNumber = "C-1", Categories = { scientific } },
            new Item { Name = "Delta Calc", ModelNumber = "D-1", Categories = { graphing } },
            new Item { Name = "Echo Calc", ModelNumber = "E-1", Categories = { scientific } }
        };
        items[2].Copies.Add(new ItemCopy { SerialNumber = "C-001", Status = CopyStatus.Available });
        items[2].Copies.Add(new ItemCopy { SerialNumber = "C-002", Status = CopyStatus.Available });
        items[2].Copies.Add(new ItemCopy { SerialNumber = "C-003", Status = CopyStatus.Loaned });

        context.Items.AddRange(items);
        await context.SaveChangesAsync();
    }

    private async Task<Core.Common.PagedResult<Core.Common.ItemWithAvailability>> QueryAsync(ItemQueryParameters q)
    {
        await using var context = _fixture.CreateContext();
        var repository = new ItemRepository(context);
        return await repository.GetPagedAsync(q, CancellationToken.None);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_ReturnsCorrectSliceAndTotalCount()
    {
        var q = new ItemQueryParameters { Page = 2, PageSize = 2 };

        var result = await QueryAsync(q);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(new[] { "Charlie Calc", "Delta Calc" }, result.Items.Select(i => i.Item.Name));
    }

    [Fact]
    public async Task GetPagedAsync_CategoryFilter_ReturnsOnlyMatching()
    {
        await using var context = _fixture.CreateContext();
        var graphingId = context.Categories.Single(c => c.Name == "Graphing").Id;
        var q = new ItemQueryParameters { CategoryId = graphingId };

        var result = await QueryAsync(q);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "Bravo Calc", "Delta Calc" }, result.Items.Select(i => i.Item.Name));
    }

    [Fact]
    public async Task GetPagedAsync_SearchIsCaseInsensitive()
    {
        var q = new ItemQueryParameters { Search = "aLpHa" };

        var result = await QueryAsync(q);

        var item = Assert.Single(result.Items);
        Assert.Equal("Alpha Calc", item.Item.Name);
    }

    [Fact]
    public async Task GetPagedAsync_AvailableCopies_CountsOnlyAvailable()
    {
        var q = new ItemQueryParameters { Search = "Charlie" };

        var result = await QueryAsync(q);

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.AvailableCopies);
    }
}