using Microsoft.Extensions.DependencyInjection;
using VatDesk.Core.Abstractions;

namespace VatDesk.Infrastructure;

/// <summary>
/// Registers a country's VAT category registry + declaration strategy under a country-code key.
/// No real country implementation exists yet (Phase 3 next session); this only proves the seam.
/// </summary>
public static class CountryServiceCollectionExtensions
{
    public static IServiceCollection AddCountry<TRegistry, TStrategy>(this IServiceCollection services, string countryCode)
        where TRegistry : class, IVatCategoryRegistry
        where TStrategy : class, IVatDeclarationStrategy
    {
        services.AddKeyedSingleton<IVatCategoryRegistry, TRegistry>(countryCode);
        services.AddKeyedSingleton<IVatDeclarationStrategy, TStrategy>(countryCode);
        return services;
    }
}
