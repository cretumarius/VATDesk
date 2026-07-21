using Microsoft.Extensions.DependencyInjection;
using VatDesk.Core.Abstractions;

namespace VatDesk.Infrastructure;

/// <summary>Registers a country's VAT category registry + declaration strategy under a country-code key.</summary>
public static class CountryServiceCollectionExtensions
{
    public static IServiceCollection AddCountry<TRegistry, TStrategy>(this IServiceCollection services, string countryCode)
        where TRegistry : class, IVatCategoryRegistry
        where TStrategy : class, IVatDeclarationStrategy
    {
        services.AddKeyedSingleton<IVatCategoryRegistry, TRegistry>(countryCode);

        // TStrategy takes a plain IVatCategoryRegistry constructor parameter (kept DI-attribute-free
        // and independently testable, see HungarianVatDeclarationStrategyTests). Only a *keyed*
        // registry is registered above, and .NET DI does not thread keyed services through implicit
        // constructor injection, so the keyed registry is resolved explicitly here and handed in.
        services.AddKeyedSingleton<IVatDeclarationStrategy>(countryCode, (sp, key) =>
            ActivatorUtilities.CreateInstance<TStrategy>(sp, sp.GetRequiredKeyedService<IVatCategoryRegistry>(key!)));

        return services;
    }
}
