using CalculatorLending.Core.Options;
using CalculatorLending.Data.Context;
using CalculatorLending.Data.Seed;
using CalculatorLending.Service.Security;
using Microsoft.EntityFrameworkCore;

namespace CalculatorLending.Tests.Integration;

[Collection("Postgres")]
public class SeedTests : IAsyncLifetime
{
    // Test-only values, not the real seed settings
    private static readonly SeedOptions Options = new()
    {
        AdminFullName = "Test Admin",
        AdminEmail = "admin@example.com",
        AdminPassword = "TestAdmin12345",
        DemoMemberEmail = "member@example.com",
        DemoMemberPassword = "TestMember12345"
    };

    private readonly PostgresFixture _fixture;

    public SeedTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedAsync()
    {
        await using var context = _fixture.CreateContext();
        var initializer = new DatabaseInitializer(
            context,
            new PasswordHasher(),
            Microsoft.Extensions.Options.Options.Create(Options));

        await initializer.SeedReferenceDataAsync();
        await initializer.SeedDemoDataAsync();
    }

    private static async Task<int[]> CountRowsAsync(AppDbContext context) => new[]
    {
        await context.Categories.CountAsync(),
        await context.Members.CountAsync(),
        await context.Items.CountAsync(),
        await context.ItemCopies.CountAsync(),
        await context.Items.SelectMany(i => i.Categories).CountAsync()
    };

    [Fact]
    public async Task Seed_RunTwice_DoesNotCreateDuplicates()
    {
        await SeedAsync();
        await using var afterFirst = _fixture.CreateContext();
        var firstCounts = await CountRowsAsync(afterFirst);

        await SeedAsync();

        await using var afterSecond = _fixture.CreateContext();
        Assert.Equal(firstCounts, await CountRowsAsync(afterSecond));
    }

    [Fact]
    public async Task Seed_StoresHashedAdminPassword()
    {
        await SeedAsync();

        await using var context = _fixture.CreateContext();
        var admin = await context.Members.SingleAsync(m => m.Email == "admin@example.com");
        Assert.NotEqual(Options.AdminPassword, admin.PasswordHash);
        Assert.True(new PasswordHasher().Verify(Options.AdminPassword, admin.PasswordHash));
    }
}