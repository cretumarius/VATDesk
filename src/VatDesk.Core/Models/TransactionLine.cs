namespace VatDesk.Core.Models;

/// <summary>Canonical internal record both parsers (CSV, NAV XML) normalize into.</summary>
public record TransactionLine(
    string InvoiceNumber,
    DateOnly IssueDate,
    string? PartnerName,
    string? PartnerTaxNumber,
    Direction Direction,
    decimal NetAmount,
    string VatCode,
    decimal VatAmount,
    decimal GrossAmount,
    string Currency,
    int SourceRowNumber);
