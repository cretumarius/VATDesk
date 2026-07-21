using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Infrastructure.Parsing;

/// <summary>CSV parser per data-contract.md section 1. Applies V1 (required/well-formed) per row.</summary>
public class CsvInvoiceParser : IInvoiceParser
{
    private static readonly string[] RequiredColumns = ["invoicenumber", "issuedate", "netamount", "vatcode", "vatamount", "grossamount"];

    private static readonly HashSet<string> KnownColumns =
    [
        "invoicenumber", "issuedate", "partnername", "partnertaxnumber",
        "direction", "netamount", "vatcode", "vatamount", "grossamount", "currency",
    ];

    public bool CanParse(Stream content) => !ContentSniffing.FirstNonWhitespaceCharIsLessThan(content);

    public Task<ParseResult> ParseAsync(Stream content, CancellationToken cancellationToken = default)
    {
        content.Position = 0;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null || csv.HeaderRecord.Length == 0)
        {
            throw new InvoiceParseException("CSV file has no header row.");
        }

        var headerMap = BuildHeaderMap(csv.HeaderRecord);
        var missingColumns = RequiredColumns.Where(c => !headerMap.ContainsKey(c)).ToList();
        if (missingColumns.Count > 0)
        {
            throw new InvoiceParseException($"CSV is missing required column(s): {string.Join(", ", missingColumns)}.");
        }

        var lines = new List<TransactionLine>();
        var issues = new List<ValidationIssue>();

        var hasDirectionColumn = headerMap.ContainsKey("direction");
        if (!hasDirectionColumn)
        {
            issues.Add(new ValidationIssue(0, ValidationRuleIds.FileNotice, Severity.Info,
                "Direction column is missing; all rows are treated as OUT."));
        }

        var unknownColumns = csv.HeaderRecord
            .Select(h => h.Trim())
            .Where(h => !KnownColumns.Contains(h.ToLowerInvariant()))
            .ToList();
        if (unknownColumns.Count > 0)
        {
            issues.Add(new ValidationIssue(0, ValidationRuleIds.FileNotice, Severity.Info,
                $"Unrecognized column(s) ignored: {string.Join(", ", unknownColumns)}."));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        while (csv.Read())
        {
            ParseRow(csv, headerMap, hasDirectionColumn, today, lines, issues);
        }

        return Task.FromResult(new ParseResult(SourceFormat.Csv, lines, issues));
    }

    private static void ParseRow(
        CsvReader csv,
        IReadOnlyDictionary<string, int> headerMap,
        bool hasDirectionColumn,
        DateOnly today,
        List<TransactionLine> lines,
        List<ValidationIssue> issues)
    {
        var rowNumber = csv.Context.Parser!.Row;
        var rowErrors = new List<string>();

        string GetRaw(string column) =>
            headerMap.TryGetValue(column, out var index) ? csv.GetField(index) ?? string.Empty : string.Empty;

        var invoiceNumber = GetRaw("invoicenumber").Trim();
        if (invoiceNumber.Length == 0)
        {
            rowErrors.Add("InvoiceNumber is required.");
        }
        else if (invoiceNumber.Length > 50)
        {
            rowErrors.Add($"InvoiceNumber must be at most 50 characters (got {invoiceNumber.Length}).");
        }

        var issueDate = default(DateOnly);
        if (!FieldParsing.TryParseIsoDate(GetRaw("issuedate"), out issueDate, out var dateError))
        {
            rowErrors.Add($"IssueDate {dateError}.");
        }
        else if (issueDate > today.AddDays(1))
        {
            rowErrors.Add($"IssueDate cannot be more than 1 day in the future (got {issueDate:yyyy-MM-dd}, today is {today:yyyy-MM-dd}).");
        }

        string? partnerName = null;
        if (headerMap.ContainsKey("partnername"))
        {
            var raw = GetRaw("partnername").Trim();
            if (raw.Length > 200)
            {
                rowErrors.Add($"PartnerName must be at most 200 characters (got {raw.Length}).");
            }
            else
            {
                partnerName = raw.Length == 0 ? null : raw;
            }
        }

        var partnerTaxNumberRaw = GetRaw("partnertaxnumber").Trim();
        string? partnerTaxNumber = partnerTaxNumberRaw.Length == 0 ? null : partnerTaxNumberRaw;

        var direction = Direction.Out;
        if (hasDirectionColumn)
        {
            var raw = GetRaw("direction").Trim();
            if (raw.Length > 0 && !TryParseDirection(raw, out direction))
            {
                rowErrors.Add($"Direction must be 'OUT' or 'IN' (got '{raw}').");
            }
        }

        FieldParsing.TryParseNonNegativeDecimal(GetRaw("netamount"), out var netAmount, out var netError);
        if (netError is not null)
        {
            rowErrors.Add($"NetAmount {netError}.");
        }

        var vatCode = GetRaw("vatcode").Trim();
        if (vatCode.Length == 0)
        {
            rowErrors.Add("VatCode is required.");
        }

        FieldParsing.TryParseNonNegativeDecimal(GetRaw("vatamount"), out var vatAmount, out var vatError);
        if (vatError is not null)
        {
            rowErrors.Add($"VatAmount {vatError}.");
        }

        FieldParsing.TryParseNonNegativeDecimal(GetRaw("grossamount"), out var grossAmount, out var grossError);
        if (grossError is not null)
        {
            rowErrors.Add($"GrossAmount {grossError}.");
        }

        var currency = "HUF";
        var currencyRaw = GetRaw("currency").Trim();
        if (currencyRaw.Length > 0)
        {
            currency = currencyRaw.ToUpperInvariant();
        }

        if (rowErrors.Count > 0)
        {
            issues.AddRange(rowErrors.Select(error => new ValidationIssue(rowNumber, ValidationRuleIds.RequiredFields, Severity.Error, error)));
            return;
        }

        lines.Add(new TransactionLine(
            invoiceNumber,
            issueDate,
            partnerName,
            partnerTaxNumber,
            direction,
            netAmount,
            vatCode,
            vatAmount,
            grossAmount,
            currency,
            rowNumber));
    }

    private static bool TryParseDirection(string raw, out Direction direction)
    {
        if (string.Equals(raw, "OUT", StringComparison.OrdinalIgnoreCase))
        {
            direction = Direction.Out;
            return true;
        }

        if (string.Equals(raw, "IN", StringComparison.OrdinalIgnoreCase))
        {
            direction = Direction.In;
            return true;
        }

        direction = Direction.Out;
        return false;
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < header.Count; i++)
        {
            var key = header[i].Trim().ToLowerInvariant();
            map.TryAdd(key, i);
        }

        return map;
    }
}
