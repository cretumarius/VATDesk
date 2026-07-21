namespace VatDesk.Core.Abstractions;

/// <summary>
/// Thrown when a whole file cannot be processed at all (empty, oversized, wrong format,
/// unreadable structure, XML hardening rejection). Distinct from per-row parse problems,
/// which are reported as ValidationIssue entries and do not fail the whole file.
/// </summary>
public class InvoiceParseException(string message) : Exception(message);
