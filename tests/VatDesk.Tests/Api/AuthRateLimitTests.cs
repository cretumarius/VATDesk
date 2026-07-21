using System.Net;
using System.Net.Http.Json;
using VatDesk.Api.Dtos;

namespace VatDesk.Tests.Api;

/// <summary>
/// Isolated in its own factory (not shared via IClassFixture with any other test class),
/// so its rapid-fire login attempts don't interfere with — or get starved by — the login
/// calls other test classes make against their own separate factory instances.
/// </summary>
public class AuthRateLimitTests : IClassFixture<VatDeskWebApplicationFactory>
{
    private readonly VatDeskWebApplicationFactory _factory;

    public AuthRateLimitTests(VatDeskWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_RapidAttempts_TriggersRateLimitAfterPermitLimit()
    {
        using var client = _factory.CreateClient();
        var statuses = new List<HttpStatusCode>();

        // Matches Program.cs's login policy: PermitLimit = 8 per 1-minute window per IP.
        for (var i = 0; i < 9; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("nobody@demo.hu", "wrong"));
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(8), status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[8]);
    }
}
