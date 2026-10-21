using CalculatorLending.Core.Interfaces.Security;
using CalculatorLending.Core.Interfaces.Services;
using CalculatorLending.Service.Mapping;
using CalculatorLending.Service.Security;
using CalculatorLending.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CalculatorLending.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // Scoped: these depend on the repositories, which are Scoped
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ILoanService, LoanService>();

        // Singleton: stateless and thread-safe
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        // The real clock (tests replace it with a fake one)
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
