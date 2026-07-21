using System.Net;
using VatDesk.Core.Models;

namespace VatDesk.Tests.Api;

/// <summary>
/// Isolated in its own factory (not shared via IClassFixture with any other test class),
/// same reasoning as AuthRateLimitTests: repeatedly hitting the same rate-limited endpoint
/// here must not eat into — or get starved by — the upload budget other test classes'
/// uploads consume against their own separate factory instances.
/// </summary>
public class UploadRateLimitTests : IClassFixture<VatDeskWebApplicationFactory>
{
    private readonly VatDeskWebApplicationFactory _factory;

    public UploadRateLimitTests(VatDeskWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_RapidAttempts_TriggersRateLimitAfterPermitLimit()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);
        var statuses = new List<HttpStatusCode>();

        // Matches Program.cs's upload policy: PermitLimit = 20 per 1-minute window per
        // authenticated user. Reuses an intentionally-empty body on every attempt — an
        // empty-file 400 still executes the endpoint (and its [EnableRateLimiting]) same
        // as a real upload would, without needing 21 real parses.
        for (var i = 0; i < 21; i++)
        {
            using var content = new MultipartFormDataContent();
            using var response = await client.PostAsync("/api/declarations", content);
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(20), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[20]);
    }
}
