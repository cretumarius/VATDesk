using System.Globalization;

namespace VatDesk.Infrastructure.Parsing;

/// <summary>Shared invariant-culture field parsing for row-level values, with expected-vs-actual error text.</summary>
internal static class FieldParsing
{
    public static bool TryParseInvariantDecimal(string raw, out decimal value, out string? error)
    {
        value = default;
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            error = "is required";
            return false;
        }

        if (!decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
        {
            error = trimmed.Contains(',')
                ? $"must use '.' as the decimal separator, not ',' (use 27000.50, not 27000,50; got '{trimmed}')"
                : $"must be a valid decimal (got '{trimmed}')";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryParseNonNegativeDecimal(string raw, out decimal value, out string? error)
    {
        if (!TryParseInvariantDecimal(raw, out value, out error))
        {
            return false;
        }

        if (value < 0m)
        {
            error = $"must be >= 0 (got {value.ToString(CultureInfo.InvariantCulture)})";
            return false;
        }

        return true;
    }

    public static bool TryParseIsoDate(string raw, out DateOnly value, out string? error)
    {
        value = default;
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            error = "is required";
            return false;
        }

        if (!DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = $"must be in yyyy-MM-dd format (got '{trimmed}')";
            return false;
        }

        error = null;
        return true;
    }
}
