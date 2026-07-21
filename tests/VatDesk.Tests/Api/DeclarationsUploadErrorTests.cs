using System.Net;
using System.Net.Http.Headers;
using VatDesk.Core.Models;
using VatDesk.Infrastructure.Parsing;

namespace VatDesk.Tests.Api;

/// <summary>
/// Empirically verifies every upload rejection path returns ProblemDetails, never a bare
/// 500 — see Stage A of the 4.2 session prompt.
/// </summary>
public class DeclarationsUploadErrorTests : IClassFixture<VatDeskWebApplicationFactory>
{
    private readonly VatDeskWebApplicationFactory _factory;

    public DeclarationsUploadErrorTests(VatDeskWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_OversizedFile_ReturnsProblemDetailsNot500()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);
        var oversized = new byte[ParserFactory.MaxFileSizeBytes + 1024];

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(oversized);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "oversized.csv");

        var response = await client.PostAsync("/api/declarations", content);

        // Verified against real Kestrel (not just TestServer) in docker compose: exceeding
        // FormOptions.MultipartBodyLengthLimit fails form reading, which [ApiController]'s
        // automatic model-state validation turns into a 400 ProblemDetails — no custom
        // handling needed, already never a bare 500.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_RandomBinaryContentWithCsvExtension_Returns400NotServerError()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        // Genuinely non-text bytes (a fake PNG header), not just malformed CSV — exercises
        // the path where content isn't even valid UTF-8/ASCII.
        var binary = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD };

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(binary);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "renamed-image.csv");

        var response = await client.PostAsync("/api/declarations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Upload_EmptyFile_Returns400ProblemDetails()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "empty.csv");

        var response = await client.PostAsync("/api/declarations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Upload_WrongExtension_Returns400ProblemDetails()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([0x01, 0x02, 0x03]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "invoice.png");

        var response = await client.PostAsync("/api/declarations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
