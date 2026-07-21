using Microsoft.EntityFrameworkCore;
using VatDesk.Infrastructure.Persistence;

namespace VatDesk.Api.Extensions;

/// <summary>One-time startup tasks that run after the app is built but before the pipeline serves any request.</summary>
public static class WebApplicationStartupExtensions
{
    /// <summary>Applies pending EF Core migrations. Same execution point and failure behavior as before this was extracted: runs once at startup, and an exception here still crashes the process before it ever starts listening.</summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VatDeskDbContext>();

        // IsRelational() guard lets integration tests swap in the EF Core InMemory provider
        // (which doesn't support migrations) without touching this startup path.
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }
    }
}
