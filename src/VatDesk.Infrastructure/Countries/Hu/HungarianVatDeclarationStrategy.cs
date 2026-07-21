using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Infrastructure.Countries.Hu;

/// <summary>
/// Aggregates error-free rows into a DeclarationSummary per data-contract.md section 5.
/// Rows with errors (from the parser's V1 issues or this run's V2-V8 issues) are excluded
/// from totals; rows with only warnings are included using their declared amounts.
/// </summary>
public class HungarianVatDeclarationStrategy(IVatCategoryRegistry registry) : IVatDeclarationStrategy
{
    public string CountryCode => "HU";

    public DeclarationSummary BuildDeclaration(IReadOnlyList<TransactionLine> lines, IReadOnlyList<ValidationIssue> parserIssues, SourceFormat format)
    {
        // NAV XML holds exactly one invoice per file; its lines legitimately share one
        // InvoiceNumber/Direction by design, so V8 duplicate detection would misfire on them.
        var checkDuplicates = format != SourceFormat.NavXml;
        var validatorIssues = TransactionLineValidator.Validate(lines, registry, checkDuplicates);
        var allIssues = parserIssues.Concat(validatorIssues).ToList();

        var issuesByRow = allIssues
            .Where(i => i.RowNumber != 0) // exclude file-level notices from per-row classification
            .GroupBy(i => i.RowNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        var errorRowNumbers = issuesByRow
            .Where(kv => kv.Value.Any(i => i.Severity == Severity.Error))
            .Select(kv => kv.Key)
            .ToHashSet();

        // V3 (non-HUF currency) is a Warning but data-contract.md marks it "row skipped from
        // totals" — unlike other warnings (V5/V6/V7/V8), which use their declared amounts.
        var nonHufRowNumbers = issuesByRow
            .Where(kv => kv.Value.Any(i => i.RuleId == ValidationRuleIds.NonHufCurrency))
            .Select(kv => kv.Key)
            .ToHashSet();

        var excludedFromTotals = errorRowNumbers.Union(nonHufRowNumbers).ToHashSet();
        var includedLines = lines.Where(l => !excludedFromTotals.Contains(l.SourceRowNumber)).ToList();

        var perCategory = includedLines
            .GroupBy(l => (l.VatCode, l.Direction))
            .Select(g => new CategoryTotal(
                g.Key.VatCode,
                g.Key.Direction,
                g.Count(),
                g.Sum(l => l.NetAmount),
                g.Sum(l => l.VatAmount),
                g.Sum(l => l.GrossAmount)))
            .OrderBy(c => c.Direction) // Out (0) before In (1)
            .ThenBy(c => registry.TryGet(c.VatCode, out var category) ? category.SortOrder : int.MaxValue)
            .ToList();

        var totalOutputVat = includedLines.Where(l => l.Direction == Direction.Out).Sum(l => l.VatAmount);
        var totalDeductibleInputVat = includedLines.Where(l => l.Direction == Direction.In).Sum(l => l.VatAmount);

        var warningRowNumbers = issuesByRow.Keys.Except(errorRowNumbers).ToHashSet();
        var validRows = lines.Count(l => !issuesByRow.ContainsKey(l.SourceRowNumber));

        var validation = new ValidationSummary(
            ValidRows: validRows,
            WarningRows: warningRowNumbers.Count,
            ErrorRows: errorRowNumbers.Count,
            Issues: allIssues.OrderBy(i => i.RowNumber).ThenBy(i => i.RuleId, StringComparer.Ordinal).ToList());

        return new DeclarationSummary(
            perCategory,
            totalOutputVat,
            totalDeductibleInputVat,
            totalOutputVat - totalDeductibleInputVat,
            validation);
    }
}
