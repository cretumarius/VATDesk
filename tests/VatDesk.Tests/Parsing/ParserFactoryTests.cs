using System.Text;
using VatDesk.Core.Abstractions;
using VatDesk.Infrastructure.Parsing;

namespace VatDesk.Tests.Parsing;

public class ParserFactoryTests
{
    private readonly ParserFactory _factory = new([new CsvInvoiceParser(), new NavXmlInvoiceParser()]);

    [Fact]
    public async Task CsvContent_SniffedAndParsedAsCsv()
    {
        await using var stream = SkillFixtures.OpenRead("sample-clean.csv");

        var result = await _factory.ParseAsync(stream);

        Assert.Equal(10, result.Lines.Count);
    }

    [Fact]
    public async Task XmlContent_SniffedAndParsedAsXml()
    {
        await using var stream = SkillFixtures.OpenRead("sample-nav.xml");

        var result = await _factory.ParseAsync(stream);

        Assert.Equal(3, result.Lines.Count);
    }

    [Fact]
    public async Task LeadingWhitespaceBeforeXmlDeclaration_StillSniffedAsXml()
    {
        // No XML declaration: it's optional, and one may not be preceded by whitespace.
        var xml = "   \n\t<InvoiceData xmlns=\"http://schemas.nav.gov.hu/OSA/3.0/data\"><invoiceNumber>X</invoiceNumber><invoiceIssueDate>2026-01-01</invoiceIssueDate><invoiceLines></invoiceLines></InvoiceData>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var result = await _factory.ParseAsync(stream);

        Assert.Empty(result.Lines);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task EmptyFile_Throws()
    {
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<InvoiceParseException>(() => _factory.ParseAsync(stream));
    }

    [Fact]
    public async Task OversizedFile_Throws()
    {
        using var stream = new MemoryStream(new byte[ParserFactory.MaxFileSizeBytes + 1]);

        await Assert.ThrowsAsync<InvoiceParseException>(() => _factory.ParseAsync(stream));
    }

    [Fact]
    public async Task UnrecognizableContent_Throws()
    {
        // Neither XML (no leading '<') nor a CSV the required-columns check can accept.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not,a,valid,header\n1,2,3,4"));

        await Assert.ThrowsAsync<InvoiceParseException>(() => _factory.ParseAsync(stream));
    }
}
