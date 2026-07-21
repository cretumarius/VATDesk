using Microsoft.EntityFrameworkCore;
using VatDesk.Infrastructure.Persistence;
using VatDesk.Infrastructure.Persistence.Repositories;

namespace VatDesk.Api.Extensions;

/// <summary>EF Core + Npgsql DbContext and repositories. Migrations are applied at startup — see WebApplicationExtensions.ApplyMigrationsAsync, not here.</summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VatDeskDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<DeclarationRepository>();
        services.AddScoped<UserRepository>();

        return services;
    }
}
