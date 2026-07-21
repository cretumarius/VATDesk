using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Core.Abstractions;

/// <summary>Per-country aggregation + validation math. Controllers must never contain this logic.</summary>
public interface IVatDeclarationStrategy
{
    string CountryCode { get; }

    DeclarationSummary BuildDeclaration(
        IReadOnlyList<TransactionLine> lines,
        IReadOnlyList<ValidationIssue> parserIssues);
}
