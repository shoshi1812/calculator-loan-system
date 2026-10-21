namespace CalculatorLending.Core.Interfaces.Data;

public interface IDatabaseInitializer
{
    Task MigrateAsync(CancellationToken ct = default);
    Task SeedReferenceDataAsync(CancellationToken ct = default);
    Task SeedDemoDataAsync(CancellationToken ct = default);
}