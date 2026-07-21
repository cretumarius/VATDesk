namespace VatDesk.Core.Validation;

public record ValidationIssue(
    int RowNumber,
    string RuleId,
    Severity Severity,
    string Message);
