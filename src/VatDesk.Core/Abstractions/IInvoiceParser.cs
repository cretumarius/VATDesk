using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Core.Abstractions;

public record ParseResult(
    SourceFormat Format,
    IReadOnlyList<TransactionLine> Lines,
    IReadOnlyList<ValidationIssue> Issues);

/// <summary>Implemented per input format (CSV, NAV XML); format is chosen by content sniffing, not extension.</summary>
public interface IInvoiceParser
{
    bool CanParse(Stream content);

    Task<ParseResult> ParseAsync(Stream content, CancellationToken cancellationToken = default);
}
