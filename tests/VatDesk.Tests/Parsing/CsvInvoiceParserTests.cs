using VatDesk.Core.Validation;
using VatDesk.Infrastructure.Countries.Hu;
using VatDesk.Infrastructure.Parsing;

namespace VatDesk.Tests.Parsing;

public class CsvInvoiceParserTests
{
    private readonly CsvInvoiceParser _parser = new();
    private readonly HungarianVatCategoryRegistry _registry = new();

    [Fact]
    public void CanParse_CsvContent_ReturnsTrue()
    {
        using var stream = SkillFixtures.OpenRead("sample-clean.csv");
        Assert.True(_parser.CanParse(stream));
    }

    [Fact]
    public async Task SampleClean_ParsesTenLines_WithZeroIssues()
    {
        await using var stream = SkillFixtures.OpenRead("sample-clean.csv");

        var result = await _parser.ParseAsync(stream);

        Assert.Equal(10, result.Lines.Count);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task SampleInvalid_EachDirtyRowTriggersExactlyOneRule()
    {
        await using var stream = SkillFixtures.OpenRead("sample-invalid.csv");
        var parseResult = await _parser.ParseAsync(stream);

        var validatorIssues = TransactionLineValidator.Validate(parseResult.Lines, _registry);
        var byRow = parseResult.Issues.Concat(validatorIssues)
            .Where(i => i.RowNumber != 0)
            .GroupBy(i => i.RowNumber)
            .ToDictionary(g => g.Key, g => g.ToList());

        Assert.DoesNotContain(2, byRow.Keys); // control row: fully clean

        AssertSingleRule(byRow, 3, ValidationRuleIds.RequiredFields); // missing invoice number
        AssertSingleRule(byRow, 4, ValidationRuleIds.RequiredFields); // bad date format
        AssertSingleRule(byRow, 5, ValidationRuleIds.UnknownVatCode); // unknown code 99
        AssertSingleRule(byRow, 6, ValidationRuleIds.NonZeroVatOnExemptCode); // AAM with nonzero VAT
        AssertSingleRule(byRow, 7, ValidationRuleIds.VatAmountMismatch); // VAT/rate mismatch
        AssertSingleRule(byRow, 8, ValidationRuleIds.GrossAmountMismatch); // gross mismatch
        AssertSingleRule(byRow, 9, ValidationRuleIds.PartnerTaxNumberFormat); // bad tax number
        AssertSingleRule(byRow, 10, ValidationRuleIds.NonHufCurrency); // EUR invoice
        AssertSingleRule(byRow, 11, ValidationRuleIds.DuplicateInvoiceNumber); // duplicate of row 2
        AssertSingleRule(byRow, 12, ValidationRuleIds.RequiredFields); // comma decimal

        // rows 3, 4, 12 fail structurally (V1) and are excluded from Lines entirely
        Assert.DoesNotContain(parseResult.Lines, l => l.SourceRowNumber is 3 or 4 or 12);

        // rows 5-11 parse fine structurally; only downstream V2-V8 flags them
        Assert.Contains(parseResult.Lines, l => l.SourceRowNumber == 5);
        Assert.Contains(parseResult.Lines, l => l.SourceRowNumber == 11);
    }

    private static void AssertSingleRule(Dictionary<int, List<ValidationIssue>> byRow, int row, string expectedRuleId)
    {
        Assert.True(byRow.TryGetValue(row, out var issues), $"Expected exactly one issue on row {row}.");
        var issue = Assert.Single(issues!);
        Assert.Equal(expectedRuleId, issue.RuleId);
    }
}
