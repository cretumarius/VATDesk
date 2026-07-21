using System.Net;
using VatDesk.Core.Models;

namespace VatDesk.Tests.Api;

public class SamplesApiTests : IClassFixture<VatDeskWebApplicationFactory>
{
    private readonly VatDeskWebApplicationFactory _factory;

    public SamplesApiTests(VatDeskWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCleanCsv_IsByteIdenticalToSkillAsset()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Viewer);

        var response = await client.GetAsync("/api/samples/clean.csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("clean.csv", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));

        var served = await response.Content.ReadAsByteArrayAsync();
        var expected = await File.ReadAllBytesAsync(SkillFixtures.Path("sample-clean.csv"));
        Assert.Equal(expected, served);
    }

    [Fact]
    public async Task GetInvalidCsv_IsByteIdenticalToSkillAsset()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        var response = await client.GetAsync("/api/samples/invalid.csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var served = await response.Content.ReadAsByteArrayAsync();
        var expected = await File.ReadAllBytesAsync(SkillFixtures.Path("sample-invalid.csv"));
        Assert.Equal(expected, served);
    }

    [Fact]
    public async Task GetNavXml_IsByteIdenticalToSkillAsset()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        var response = await client.GetAsync("/api/samples/nav.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        var served = await response.Content.ReadAsByteArrayAsync();
        var expected = await File.ReadAllBytesAsync(SkillFixtures.Path("sample-nav.xml"));
        Assert.Equal(expected, served);
    }

    [Fact]
    public async Task GetSample_Anonymous_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/samples/clean.csv");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
