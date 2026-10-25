using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CalculatorLending.Tests.Integration;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Running every migration on an empty database also tests the migrations themselves
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        // Table names come from the EF model, so the many-to-many link table is included too
        var tables = context.Model.GetEntityTypes()
            .Select(e => (Schema: e.GetSchema(), Table: e.GetTableName()))
            .Where(t => t.Table is not null)
            .Distinct()
            .Select(t => t.Schema is null ? $"\"{t.Table}\"" : $"\"{t.Schema}\".\"{t.Table}\"");

        var sql = $"TRUNCATE TABLE {string.Join(", ", tables)} RESTART IDENTITY CASCADE;";
        await context.Database.ExecuteSqlRawAsync(sql);
    }
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

[Collection("Postgres")]
public class PostgresFixtureTests
{
    private readonly PostgresFixture _fixture;

    public PostgresFixtureTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Fixture_AfterMigrations_DatabaseIsReachableAndResettable()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();
        var canConnect = await context.Database.CanConnectAsync();

        Assert.True(canConnect);
    }
}