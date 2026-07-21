using System.Globalization;
using System.Text.RegularExpressions;
using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;

namespace VatDesk.Core.Validation;

/// <summary>
/// Runs rules V2-V8 against already-parsed lines (V1 is format-specific and lives in each
/// IInvoiceParser, since malformed raw text can never become a well-typed TransactionLine).
/// Each check is a standalone, independently testable static method; <see cref="Validate"/>
/// runs them in the order defined by data-contract.md section 4.
/// </summary>
public static partial class TransactionLineValidator
{
    public const decimal ToleranceHuf = 1m;

    // V7 tax-number format is Hungarian/EU-specific per data-contract.md; the registry model
    // (vat-categories.md) has no seam for country-specific validation beyond VAT codes, so this
    // is hardcoded for HU here. Extending to another country will need this rule pulled out of
    // the shared engine — see README "Extending to a new country" / session decisions log.
    [GeneratedRegex(@"^\d{8}-\d-\d{2}$")]
    private static partial Regex HuTaxNumberRegex();

    [GeneratedRegex(@"^[A-Z]{2}[A-Z0-9]{2,12}$")]
    private static partial Regex EuVatIdRegex();

    /// <param name="checkDuplicateInvoiceNumbers">
    /// V8 assumes one TransactionLine == one source record, true for CSV (one row = one record).
    /// A NAV XML file holds exactly one invoice (data-contract.md section 2), and all of that
    /// invoice's lines legitimately inherit the same InvoiceNumber + Direction from its header —
    /// so for XML input this check would flag every line after the first as a false-positive
    /// "duplicate". Callers pass false for XML-sourced lines to suppress V8 accordingly.
    /// </param>
    public static IReadOnlyList<ValidationIssue> Validate(
        IReadOnlyList<TransactionLine> lines,
        IVatCategoryRegistry registry,
        bool checkDuplicateInvoiceNumbers = true)
    {
        var issues = new List<ValidationIssue>();
        var seen = new HashSet<(string InvoiceNumber, Direction Direction)>();

        foreach (var line in lines)
        {
            if (CheckVatCodeKnown(line, registry, out var category) is { } v2)
            {
                issues.Add(v2);
            }

            if (CheckCurrencyIsHuf(line) is { } v3)
            {
                issues.Add(v3);
            }

            if (category is not null)
            {
                if (category.Kind == VatKind.Percentage)
                {
                    if (CheckVatAmountMatchesRate(line, category) is { } v5)
                    {
                        issues.Add(v5);
                    }
                }
                else if (CheckExemptVatAmountIsZero(line, category) is { } v4)
                {
                    issues.Add(v4);
                }
            }

            if (CheckGrossAmountMatchesNetPlusVat(line) is { } v6)
            {
                issues.Add(v6);
            }

            if (CheckPartnerTaxNumberFormat(line) is { } v7)
            {
                issues.Add(v7);
            }

            if (checkDuplicateInvoiceNumbers && CheckDuplicateInvoiceNumber(line, seen) is { } v8)
            {
                issues.Add(v8);
            }
        }

        return issues;
    }

    /// <summary>V2: VatCode must exist in the country's category registry.</summary>
    public static ValidationIssue? CheckVatCodeKnown(TransactionLine line, IVatCategoryRegistry registry, out VatCategory? category)
    {
        if (registry.TryGet(line.VatCode, out var found))
        {
            category = found;
            return null;
        }

        category = null;
        var validCodes = string.Join(", ", registry.All.OrderBy(c => c.SortOrder).Select(c => c.Code));
        return new ValidationIssue(
            line.SourceRowNumber,
            ValidationRuleIds.UnknownVatCode,
            Severity.Error,
            $"VatCode '{line.VatCode}' is not recognized. Valid codes: {validCodes}.");
    }

    /// <summary>V3: only HUF is processed in v1; other currencies are flagged and skipped from totals.</summary>
    public static ValidationIssue? CheckCurrencyIsHuf(TransactionLine line)
    {
        if (string.Equals(line.Currency, "HUF", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new ValidationIssue(
            line.SourceRowNumber,
            ValidationRuleIds.NonHufCurrency,
            Severity.Warning,
            $"Currency '{line.Currency}' is not supported in v1 (only HUF is processed); row excluded from totals.");
    }

    /// <summary>V4: exempt / zero-rated / reverse-charge codes must carry VatAmount == 0.</summary>
    public static ValidationIssue? CheckExemptVatAmountIsZero(TransactionLine line, VatCategory category)
    {
        if (line.VatAmount == 0m)
        {
            return null;
        }

        return new ValidationIssue(
            line.SourceRowNumber,
            ValidationRuleIds.NonZeroVatOnExemptCode,
            Severity.Error,
            $"VatAmount must be 0 for {category.Kind} code '{category.Code}' (got {line.VatAmount.ToString(CultureInfo.InvariantCulture)}).");
    }

    /// <summary>V5: percentage codes expect VatAmount ≈ NetAmount × rate, within ±1 HUF.</summary>
    public static ValidationIssue? CheckVatAmountMatchesRate(TransactionLine line, VatCategory category)
    {
        var expected = line.NetAmount * category.Rate!.Value;
        if (Math.Abs(line.VatAmount - expected) <= ToleranceHuf)
        {
            return null;
        }

        return new ValidationIssue(
            line.SourceRowNumber,
            ValidationRuleIds.VatAmountMismatch,
            Severity.Warning,
            $"VatAmount {line.VatAmount.ToString(CultureInfo.InvariantCulture)} does not match NetAmount × rate ({line.NetAmount.ToString(CultureInfo.InvariantCulture)} × {category.Rate!.Value.ToString("P0", CultureInfo.InvariantCulture)} = {expected.ToString(CultureInfo.InvariantCulture)}), tolerance ±{ToleranceHuf.ToString(CultureInfo.InvariantCulture)} HUF.");
    }

    /// <summary>V6: NetAmount + VatAmount ≈ GrossAmount, within ±1 HUF.</summary>
    public static ValidationIssue? CheckGrossAmountMatchesNetPlusVat(TransactionLine line)
    {
        var expected = line.NetAmount + line.VatAmount;
        if (Math.Abs(expected - line.GrossAmount) <= ToleranceHuf)
        {
            return null;
        }

        return new ValidationIssue(
            line.SourceRowNumber,
            ValidationRuleIds.GrossAmountMismatch,
            Severity.Warning,
            $"NetAmount + VatAmount ({expected.ToString(CultureInfo.InvariantCulture)}) does not match GrossAmount ({line.GrossAmount.ToString(CultureInfo.InvariantCulture)}), tolerance ±{ToleranceHuf.ToString(CultureInfo.InvariantCulture)} HUF.");
    }

    /// <summary>V7: PartnerTaxNumber, if present, must match the Hungarian or EU VAT id format.</summary>
    public static ValidationIssue? CheckPartnerTaxNumberFormat(TransactionLine line)
    {
        if (string.IsNullOrWhiteSpace(line.PartnerTaxNumber))
        {
            return null;
        }

        var value = line.PartnerTaxNumber.Trim();
        if (HuTaxNumberRegex().IsMatch(value) || EuVatIdRegex().IsMatch(value))
        {
            return null;
        }

        return new ValidationIssue(
            line.SourceRowNumber,
            ValidationRuleIds.PartnerTaxNumberFormat,
            Severity.Warning,
            $"PartnerTaxNumber '{line.PartnerTaxNumber}' does not match the Hungarian (12345678-2-41) or EU VAT id format.");
    }

    /// <summary>V8: InvoiceNumber + Direction must be unique within the file.</summary>
    public static ValidationIssue? CheckDuplicateInvoiceNumber(TransactionLine line, HashSet<(string InvoiceNumber, Direction Direction)> seen)
    {
        var key = (line.InvoiceNumber, line.Direction);
        if (seen.Add(key))
        {
            return null;
        }

        return new ValidationIssue(
            line.SourceRowNumber,
            ValidationRuleIds.DuplicateInvoiceNumber,
            Severity.Warning,
            $"Duplicate InvoiceNumber '{line.InvoiceNumber}' + Direction '{line.Direction}' also appears earlier in the file.");
    }
}
