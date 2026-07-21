namespace VatDesk.Tests.Api;

/// <summary>Security checklist item 8: X-Content-Type-Options, X-Frame-Options, and a CSP must be on every response — API JSON, static files, and the SPA fallback alike.</summary>
public class SecurityHeadersTests : IClassFixture<VatDeskWebApplicationFactory>
{
    private readonly VatDeskWebApplicationFactory _factory;

    public SecurityHeadersTests(VatDeskWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/health")] // API JSON response
    [InlineData("/theme-init.js")] // static file served via UseStaticFiles
    [InlineData("/some/client-side/route")] // SPA fallback (MapFallbackToFile)
    public async Task Response_IncludesSecurityHeaders(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp); // scripts must stay strict
    }
}
