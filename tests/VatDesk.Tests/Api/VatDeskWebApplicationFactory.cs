using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using VatDesk.Api.Dtos;
using VatDesk.Core.Models;
using VatDesk.Infrastructure.Persistence;
using VatDesk.Infrastructure.Persistence.Entities;

namespace VatDesk.Tests.Api;

/// <summary>
/// Boots the real Program with the EF Core InMemory provider swapped in for Postgres (so
/// integration tests don't require a running database) and a fixed test-only JWT signing
/// key, then seeds the same two demo users production migrations seed — InMemory never
/// runs migrations (see Program.cs's IsRelational() guard), so this stands in for that.
/// </summary>
public class VatDeskWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "test-only-signing-key-at-least-32-bytes-long-for-hs256!!";

    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ViewerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public const string AdminEmail = "admin@demo.hu";
    public const string AdminPassword = "Admin123!";
    public const string ViewerEmail = "viewer@demo.hu";
    public const string ViewerPassword = "Viewer123!";

    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private string? _adminToken;
    private string? _viewerToken;

    public VatDeskWebApplicationFactory()
    {
        // Program.cs reads builder.Configuration directly between CreateBuilder() and
        // Build() (its fail-fast Jwt:Key check) — that runs before WebApplicationFactory's
        // ConfigureWebHost/ConfigureAppConfiguration hooks ever get spliced in, so an
        // in-memory config source added there is invisible to it. An actual environment
        // variable IS visible, since WebApplicationBuilder reads env vars eagerly and this
        // is an in-process test host sharing the same process environment.
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<VatDeskDbContext>>();
            services.AddDbContext<VatDeskDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VatDeskDbContext>();
        SeedDemoUsers(dbContext);

        return host;
    }

    private static void SeedDemoUsers(VatDeskDbContext dbContext)
    {
        if (dbContext.Users.Any())
        {
            return;
        }

        var hasher = new PasswordHasher<UserEntity>();
        dbContext.Users.AddRange(
            new UserEntity
            {
                Id = AdminId,
                Email = AdminEmail,
                DisplayName = "Nagy Katalin",
                Role = UserRole.Admin,
                PasswordHash = hasher.HashPassword(null!, AdminPassword),
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new UserEntity
            {
                Id = ViewerId,
                Email = ViewerEmail,
                DisplayName = "Kovács Péter",
                Role = UserRole.Viewer,
                PasswordHash = hasher.HashPassword(null!, ViewerPassword),
                CreatedAt = DateTimeOffset.UtcNow,
            });

        dbContext.SaveChanges();
    }

    /// <summary>
    /// Logs in once per role and caches the token at the factory level (shared across every
    /// test method in a class via IClassFixture) — each test asking for an authenticated
    /// client must NOT trigger its own login call, or a handful of tests in one class would
    /// collectively trip the login rate limiter this session also adds.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(UserRole role)
    {
        await _authLock.WaitAsync();
        try
        {
            if (role == UserRole.Admin)
            {
                _adminToken ??= await LoginAsync(AdminEmail, AdminPassword);
            }
            else
            {
                _viewerToken ??= await LoginAsync(ViewerEmail, ViewerPassword);
            }
        }
        finally
        {
            _authLock.Release();
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", role == UserRole.Admin ? _adminToken : _viewerToken);
        return client;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }
}
