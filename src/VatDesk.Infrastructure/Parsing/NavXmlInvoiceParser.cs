using System.Xml;
using System.Xml.Linq;
using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Infrastructure.Parsing;

/// <summary>
/// NAV 3.0-flavored XML parser per data-contract.md section 2. XML hardening is
/// non-negotiable: DtdProcessing.Prohibit, XmlResolver = null, character cap.
/// </summary>
public class NavXmlInvoiceParser : IInvoiceParser
{
    private static readonly XNamespace Ns = "http://schemas.nav.gov.hu/OSA/3.0/data";
    private const long MaxCharactersInDocument = 5_000_000;

    public bool CanParse(Stream content) => ContentSniffing.FirstNonWhitespaceCharIsLessThan(content);

    public Task<ParseResult> ParseAsync(Stream content, CancellationToken cancellationToken = default)
    {
        content.Position = 0;

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxCharactersInDocument,
        };

        XDocument document;
        try
        {
            using var xmlReader = XmlReader.Create(content, settings);
            document = XDocument.Load(xmlReader, LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            throw new InvoiceParseException($"XML could not be parsed: {ex.Message}");
        }

        var root = document.Root;
        if (root is null || root.Name != Ns + "InvoiceData")
        {
            throw new InvoiceParseException("XML root element must be <InvoiceData> in the NAV 3.0 namespace.");
        }

        var invoiceNumber = root.Element(Ns + "invoiceNumber")?.Value.Trim();
        if (string.IsNullOrEmpty(invoiceNumber))
        {
            throw new InvoiceParseException("invoiceNumber is required.");
        }

        var issueDateRaw = root.Element(Ns + "invoiceIssueDate")?.Value ?? string.Empty;
        if (!FieldParsing.TryParseIsoDate(issueDateRaw, out var issueDate, out var dateError))
        {
            throw new InvoiceParseException($"invoiceIssueDate {dateError}.");
        }

        var supplierTaxNumber = NullIfEmpty(root.Element(Ns + "supplierTaxNumber")?.Value);
        var customerName = NullIfEmpty(root.Element(Ns + "customerName")?.Value);

        var directionRaw = root.Element(Ns + "invoiceDirection")?.Value.Trim();
        Direction direction;
        if (string.IsNullOrEmpty(directionRaw) || string.Equals(directionRaw, "OUTBOUND", StringComparison.OrdinalIgnoreCase))
        {
            direction = Direction.Out;
        }
        else if (string.Equals(directionRaw, "INBOUND", StringComparison.OrdinalIgnoreCase))
        {
            direction = Direction.In;
        }
        else
        {
            throw new InvoiceParseException($"invoiceDirection must be OUTBOUND or INBOUND (got '{directionRaw}').");
        }

        var lineElements = root.Element(Ns + "invoiceLines")?.Elements(Ns + "line").ToList() ?? [];

        var lines = new List<TransactionLine>();
        var issues = new List<ValidationIssue>();

        var sequence = 0;
        foreach (var lineElement in lineElements)
        {
            sequence++;
            var lineInfo = (IXmlLineInfo)lineElement;
            var rowNumber = lineInfo.HasLineInfo() ? lineInfo.LineNumber : sequence;

            ParseLine(lineElement, rowNumber, invoiceNumber, issueDate, customerName, supplierTaxNumber, direction, lines, issues);
        }

        return Task.FromResult(new ParseResult(SourceFormat.NavXml, lines, issues));
    }

    private static void ParseLine(
        XElement lineElement,
        int rowNumber,
        string invoiceNumber,
        DateOnly issueDate,
        string? partnerName,
        string? partnerTaxNumber,
        Direction direction,
        List<TransactionLine> lines,
        List<ValidationIssue> issues)
    {
        var errors = new List<string>();

        FieldParsing.TryParseNonNegativeDecimal(lineElement.Element(Ns + "lineNetAmount")?.Value ?? string.Empty, out var netAmount, out var netError);
        if (netError is not null)
        {
            errors.Add($"lineNetAmount {netError}.");
        }

        FieldParsing.TryParseNonNegativeDecimal(lineElement.Element(Ns + "lineGrossAmount")?.Value ?? string.Empty, out var grossAmount, out var grossError);
        if (grossError is not null)
        {
            errors.Add($"lineGrossAmount {grossError}.");
        }

        string? vatCode = null;
        var vatAmount = 0m;
        var vatDataElement = lineElement.Element(Ns + "lineVatData");

        if (vatDataElement is null)
        {
            errors.Add("lineVatData is required.");
        }
        else
        {
            var percentageElement = vatDataElement.Element(Ns + "vatPercentage");
            var exemptionElement = vatDataElement.Element(Ns + "vatExemption");

            if (percentageElement is not null && exemptionElement is not null)
            {
                errors.Add("lineVatData must contain exactly one of vatPercentage or vatExemption, not both.");
            }
            else if (percentageElement is null && exemptionElement is null)
            {
                errors.Add("lineVatData must contain exactly one of vatPercentage or vatExemption.");
            }
            else if (percentageElement is not null)
            {
                var percentageRaw = percentageElement.Value.Trim();
                vatCode = MapVatPercentageToCode(percentageRaw);
                if (vatCode is null)
                {
                    errors.Add($"vatPercentage '{percentageRaw}' does not map to a known rate (expected 0.27, 0.18, 0.05 or 0).");
                }
            }
            else
            {
                var caseAttribute = exemptionElement!.Attribute("case")?.Value.Trim();
                if (caseAttribute is "AAM" or "TAM" or "EUFAD" or "FAD")
                {
                    vatCode = caseAttribute;
                }
                else
                {
                    errors.Add($"vatExemption case must be one of AAM, TAM, EUFAD, FAD (got '{caseAttribute}').");
                }
            }

            var vatAmountRaw = vatDataElement.Element(Ns + "lineVatAmount")?.Value;
            if (!string.IsNullOrWhiteSpace(vatAmountRaw) &&
                !FieldParsing.TryParseNonNegativeDecimal(vatAmountRaw, out vatAmount, out var vatAmountError))
            {
                errors.Add($"lineVatAmount {vatAmountError}.");
            }
        }

        if (errors.Count > 0)
        {
            issues.AddRange(errors.Select(error => new ValidationIssue(rowNumber, ValidationRuleIds.RequiredFields, Severity.Error, error)));
            return;
        }

        lines.Add(new TransactionLine(
            invoiceNumber,
            issueDate,
            partnerName,
            partnerTaxNumber,
            direction,
            netAmount,
            vatCode!,
            vatAmount,
            grossAmount,
            "HUF",
            rowNumber));
    }

    private static string? MapVatPercentageToCode(string raw) => raw switch
    {
        "0.27" => "27",
        "0.18" => "18",
        "0.05" => "5",
        "0" => "0",
        _ => null,
    };

    private static string? NullIfEmpty(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
