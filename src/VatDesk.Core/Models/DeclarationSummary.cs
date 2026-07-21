using VatDesk.Core.Validation;

namespace VatDesk.Core.Models;

public record CategoryTotal(
    string VatCode,
    Direction Direction,
    int RowCount,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross);

public record ValidationSummary(
    int ValidRows,
    int WarningRows,
    int ErrorRows,
    IReadOnlyList<ValidationIssue> Issues);

public record DeclarationSummary(
    IReadOnlyList<CategoryTotal> PerCategory,
    decimal TotalOutputVat,
    decimal TotalDeductibleInputVat,
    decimal NetVatPayable,
    ValidationSummary Validation);
