using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Data.Context;
using CalculatorLending.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculatorLending.Core.Interfaces.Data;
using CalculatorLending.Data.Seed;
namespace CalculatorLending.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
       
        // AddDbContext registers AppDbContext as Scoped (one per request)
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("Default")));

        // Everything that holds the DbContext must be Scoped too
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IItemCopyRepository, ItemCopyRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Seed: migrations + initial data (called from Program.cs in stage 04)
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        return services;
    }
}
