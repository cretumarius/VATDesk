using VatDesk.Core.Abstractions;

namespace VatDesk.Infrastructure.Parsing;

/// <summary>Picks the right IInvoiceParser by content sniffing and enforces the file-level size cap.</summary>
public class ParserFactory(IEnumerable<IInvoiceParser> parsers)
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public async Task<ParseResult> ParseAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (!content.CanSeek)
        {
            throw new ArgumentException("Content stream must be seekable.", nameof(content));
        }

        if (content.Length == 0)
        {
            throw new InvoiceParseException("The uploaded file is empty.");
        }

        if (content.Length > MaxFileSizeBytes)
        {
            throw new InvoiceParseException("The uploaded file exceeds the 5 MB limit.");
        }

        content.Position = 0;
        var parser = parsers.FirstOrDefault(p => p.CanParse(content))
            ?? throw new InvoiceParseException("The uploaded file is neither valid CSV nor valid NAV XML.");

        content.Position = 0;
        return await parser.ParseAsync(content, cancellationToken);
    }
}
