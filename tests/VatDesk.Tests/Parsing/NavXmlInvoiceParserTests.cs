using System.Text;
using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Infrastructure.Parsing;

namespace VatDesk.Tests.Parsing;

public class NavXmlInvoiceParserTests
{
    private readonly NavXmlInvoiceParser _parser = new();

    [Fact]
    public void CanParse_XmlContent_ReturnsTrue()
    {
        using var stream = SkillFixtures.OpenRead("sample-nav.xml");
        Assert.True(_parser.CanParse(stream));
    }

    [Fact]
    public async Task SampleNav_ParsesThreeLines_WithCorrectCodes()
    {
        await using var stream = SkillFixtures.OpenRead("sample-nav.xml");

        var result = await _parser.ParseAsync(stream);

        Assert.Empty(result.Issues);
        Assert.Equal(3, result.Lines.Count);
        Assert.Equal(["27", "5", "TAM"], result.Lines.Select(l => l.VatCode));
        Assert.All(result.Lines, l => Assert.Equal("INV-2026-201", l.InvoiceNumber));
        Assert.All(result.Lines, l => Assert.Equal(new DateOnly(2026, 6, 18), l.IssueDate));
        Assert.All(result.Lines, l => Assert.Equal(Direction.Out, l.Direction));
        Assert.All(result.Lines, l => Assert.Equal("HUF", l.Currency));

        var exemptLine = result.Lines.Single(l => l.VatCode == "TAM");
        Assert.Equal(0m, exemptLine.VatAmount);
        Assert.Equal(50000m, exemptLine.NetAmount);
    }

    [Fact]
    public async Task DoctypeWithExternalEntity_IsRejected()
    {
        const string maliciousXml = """
            <?xml version="1.0"?>
            <!DOCTYPE InvoiceData [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <InvoiceData xmlns="http://schemas.nav.gov.hu/OSA/3.0/data">
              <invoiceNumber>&xxe;</invoiceNumber>
              <invoiceIssueDate>2026-06-01</invoiceIssueDate>
              <invoiceLines></invoiceLines>
            </InvoiceData>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(maliciousXml));

        await Assert.ThrowsAsync<InvoiceParseException>(() => _parser.ParseAsync(stream));
    }

    [Fact]
    public async Task UnrecognizedVatPercentage_IsRowError()
    {
        const string xml = """
            <?xml version="1.0"?>
            <InvoiceData xmlns="http://schemas.nav.gov.hu/OSA/3.0/data">
              <invoiceNumber>INV-BAD-1</invoiceNumber>
              <invoiceIssueDate>2026-06-01</invoiceIssueDate>
              <invoiceLines>
                <line>
                  <lineNetAmount>1000</lineNetAmount>
                  <lineVatData>
                    <vatPercentage>0.99</vatPercentage>
                    <lineVatAmount>990</lineVatAmount>
                  </lineVatData>
                  <lineGrossAmount>1990</lineGrossAmount>
                </line>
              </invoiceLines>
            </InvoiceData>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var result = await _parser.ParseAsync(stream);

        Assert.Empty(result.Lines);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(VatDesk.Core.Validation.ValidationRuleIds.RequiredFields, issue.RuleId);
        Assert.Contains("0.99", issue.Message);
    }
}
