using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VatDesk.Infrastructure.Persistence;

namespace VatDesk.Tests.Api;

/// <summary>Boots the real Program with the EF Core InMemory provider swapped in for Postgres,
/// so integration tests don't require a running database.</summary>
public class VatDeskWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<VatDeskDbContext>>();
            services.AddDbContext<VatDeskDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
