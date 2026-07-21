namespace VatDesk.Api.Dtos;

public record CategoryTotalDto(
    string VatCode,
    string Direction,
    int RowCount,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross);

public record ValidationIssueDto(
    int RowNumber,
    string RuleId,
    string Severity,
    string Message);

public record ValidationSummaryDto(
    int ValidRows,
    int WarningRows,
    int ErrorRows,
    IReadOnlyList<ValidationIssueDto> Issues);

/// <summary>Full declaration shape returned by POST /api/declarations and GET /api/declarations/{id}.</summary>
public record DeclarationDto(
    Guid Id,
    string SourceFilename,
    string SourceFormat,
    string CountryCode,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CategoryTotalDto> PerCategory,
    decimal TotalOutputVat,
    decimal TotalDeductibleInputVat,
    decimal NetVatPayable,
    ValidationSummaryDto Validation);

/// <summary>Lightweight shape for GET /api/declarations (history list).</summary>
public record DeclarationListItemDto(
    Guid Id,
    string SourceFilename,
    string SourceFormat,
    string CountryCode,
    string Status,
    decimal TotalOutputVat,
    decimal TotalDeductibleInputVat,
    decimal NetVatPayable,
    int ValidRows,
    int WarningRows,
    int ErrorRows,
    DateTimeOffset CreatedAt,
    string? CreatedByName);
