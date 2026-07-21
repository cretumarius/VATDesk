using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VatDesk.Api.Dtos;
using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;

namespace VatDesk.Tests.Api;

public class DeclarationsApiTests : IClassFixture<VatDeskWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly VatDeskWebApplicationFactory _factory;

    public DeclarationsApiTests(VatDeskWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_SampleClean_Returns200WithGoldenTotals()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        var response = await UploadAsync(client, "sample-clean.csv", "text/csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<DeclarationDto>(JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal("Completed", dto!.Status);
        Assert.Equal(94300m, dto.TotalOutputVat);
        Assert.Equal(50600m, dto.TotalDeductibleInputVat);
        Assert.Equal(43700m, dto.NetVatPayable);
        Assert.Equal(10, dto.Validation.ValidRows);
        Assert.Equal(0, dto.Validation.WarningRows);
        Assert.Equal(0, dto.Validation.ErrorRows);
        Assert.Equal(8, dto.PerCategory.Count);
    }

    [Fact]
    public async Task Upload_SampleInvalid_ReturnsCompletedWithWarningsAndRuleIds()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        var response = await UploadAsync(client, "sample-invalid.csv", "text/csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<DeclarationDto>(JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal("CompletedWithWarnings", dto!.Status);
        Assert.Equal(5, dto.Validation.WarningRows);
        Assert.Equal(5, dto.Validation.ErrorRows);
        Assert.Contains(dto.Validation.Issues, i => i.RuleId == "V2");
        Assert.Contains(dto.Validation.Issues, i => i.RuleId == "V5");
        Assert.Contains(dto.Validation.Issues, i => i.RuleId == "V8");
    }

    [Fact]
    public async Task Upload_XxeAttempt_Returns400ProblemDetails()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);
        const string maliciousXml = """
            <?xml version="1.0"?>
            <!DOCTYPE InvoiceData [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <InvoiceData xmlns="http://schemas.nav.gov.hu/OSA/3.0/data">
              <invoiceNumber>&xxe;</invoiceNumber>
              <invoiceIssueDate>2026-06-01</invoiceIssueDate>
              <invoiceLines></invoiceLines>
            </InvoiceData>
            """;

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(maliciousXml)));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
        content.Add(fileContent, "file", "malicious.xml");

        var response = await client.PostAsync("/api/declarations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Upload_AsViewer_Returns403()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Viewer);

        var response = await UploadAsync(client, "sample-clean.csv", "text/csv");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Upload_Anonymous_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await UploadAsync(client, "sample-clean.csv", "text/csv");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_Anonymous_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/declarations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_AfterUpload_IncludesTheNewDeclarationAttributedToUploader()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        var uploadResponse = await UploadAsync(client, "sample-clean.csv", "text/csv");
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DeclarationDto>(JsonOptions);

        var listResponse = await client.GetAsync("/api/declarations");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<List<DeclarationListItemDto>>(JsonOptions);
        var item = Assert.Single(list!, d => d.Id == uploaded!.Id);

        Assert.Equal("sample-clean.csv", item.SourceFilename);
        Assert.Equal(43700m, item.NetVatPayable);
        Assert.Equal("Nagy Katalin", item.CreatedByName);
    }

    [Fact]
    public async Task GetPdf_AfterUpload_ReturnsNonEmptyApplicationPdf()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);

        var uploadResponse = await UploadAsync(client, "sample-clean.csv", "text/csv");
        var dto = await uploadResponse.Content.ReadFromJsonAsync<DeclarationDto>(JsonOptions);

        var pdfResponse = await client.GetAsync($"/api/declarations/{dto!.Id}/pdf");

        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    /// <summary>Security checklist item 9: an unhandled exception must never reach the client as a stack trace or exception message — only a generic ProblemDetails body, with the real detail logged server-side instead.</summary>
    [Fact]
    public async Task GetPdf_WhenRendererThrows_Returns500WithoutStackTraceOrExceptionMessage()
    {
        using var setupClient = await _factory.CreateAuthenticatedClientAsync(UserRole.Admin);
        var uploadResponse = await UploadAsync(setupClient, "sample-clean.csv", "text/csv");
        var dto = await uploadResponse.Content.ReadFromJsonAsync<DeclarationDto>(JsonOptions);

        const string secretExceptionMessage = "boom: /etc/shadow read failed at line 42";
        await using var throwingFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.Replace(ServiceDescriptor.Singleton<IReportRenderer>(
                    new ThrowingReportRenderer(secretExceptionMessage)))));

        using var client = throwingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = setupClient.DefaultRequestHeaders.Authorization;

        var response = await client.GetAsync($"/api/declarations/{dto!.Id}/pdf");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(secretExceptionMessage, body);
        Assert.DoesNotContain(nameof(ThrowingReportRenderer), body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" at ", body); // shape of a serialized .NET stack frame
    }

    private class ThrowingReportRenderer(string message) : IReportRenderer
    {
        public Task<byte[]> RenderAsync(
            DeclarationSummary summary,
            DeclarationMetadata metadata,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(message);
    }

    [Fact]
    public async Task GetVatCategories_Hu_ReturnsEightCategories()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Viewer);

        var response = await client.GetAsync("/api/countries/HU/vat-categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<VatCategoryDto>>(JsonOptions);
        Assert.Equal(8, categories!.Count);
    }

    [Fact]
    public async Task GetVatCategories_UnknownCountry_Returns404()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(UserRole.Viewer);

        var response = await client.GetAsync("/api/countries/RO/vat-categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string fixtureFileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        var bytes = await File.ReadAllBytesAsync(SkillFixtures.Path(fixtureFileName));
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fixtureFileName);

        return await client.PostAsync("/api/declarations", content);
    }
}
