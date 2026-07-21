using VatDesk.Core.Models;
using VatDesk.Infrastructure.Countries.Hu;
using VatDesk.Infrastructure.Parsing;

namespace VatDesk.Tests.Countries.Hu;

public class HungarianVatDeclarationStrategyTests
{
    private readonly HungarianVatCategoryRegistry _registry = new();
    private readonly CsvInvoiceParser _parser = new();

    [Fact]
    public async Task SampleClean_ProducesExactGoldenTotals()
    {
        var strategy = new HungarianVatDeclarationStrategy(_registry);
        await using var stream = SkillFixtures.OpenRead("sample-clean.csv");
        var parseResult = await _parser.ParseAsync(stream);

        var summary = strategy.BuildDeclaration(parseResult.Lines, parseResult.Issues);

        Assert.Equal(94300m, summary.TotalOutputVat);
        Assert.Equal(50600m, summary.TotalDeductibleInputVat);
        Assert.Equal(43700m, summary.NetVatPayable);
        Assert.Equal(10, summary.Validation.ValidRows);
        Assert.Equal(0, summary.Validation.WarningRows);
        Assert.Equal(0, summary.Validation.ErrorRows);
        Assert.Empty(summary.Validation.Issues);

        AssertCategory(summary, "27", Direction.Out, 2, 300000m, 81000m, 381000m);
        AssertCategory(summary, "18", Direction.Out, 1, 60000m, 10800m, 70800m);
        AssertCategory(summary, "5", Direction.Out, 1, 50000m, 2500m, 52500m);
        AssertCategory(summary, "0", Direction.Out, 1, 120000m, 0m, 120000m);
        AssertCategory(summary, "AAM", Direction.Out, 1, 30000m, 0m, 30000m);
        AssertCategory(summary, "EUFAD", Direction.Out, 1, 200000m, 0m, 200000m);
        AssertCategory(summary, "27", Direction.In, 2, 180000m, 48600m, 228600m);
        AssertCategory(summary, "5", Direction.In, 1, 40000m, 2000m, 42000m);

        Assert.Equal(8, summary.PerCategory.Count);
    }

    [Fact]
    public async Task SampleClean_PerCategory_IsOrderedOutBlockThenInBlock_BySortOrder()
    {
        var strategy = new HungarianVatDeclarationStrategy(_registry);
        await using var stream = SkillFixtures.OpenRead("sample-clean.csv");
        var parseResult = await _parser.ParseAsync(stream);

        var summary = strategy.BuildDeclaration(parseResult.Lines, parseResult.Issues);

        var ordering = summary.PerCategory.Select(c => (c.VatCode, c.Direction)).ToList();

        Assert.Equal(
            new (string, Direction)[]
            {
                ("27", Direction.Out), ("18", Direction.Out), ("5", Direction.Out),
                ("0", Direction.Out), ("AAM", Direction.Out), ("EUFAD", Direction.Out),
                ("27", Direction.In), ("5", Direction.In),
            },
            ordering);
    }

    [Fact]
    public async Task SampleInvalid_ExcludesErrorAndNonHufRows_IncludesOtherWarningRowsInTotals()
    {
        var strategy = new HungarianVatDeclarationStrategy(_registry);
        await using var stream = SkillFixtures.OpenRead("sample-invalid.csv");
        var parseResult = await _parser.ParseAsync(stream);

        var summary = strategy.BuildDeclaration(parseResult.Lines, parseResult.Issues);

        // Row classification is severity-based regardless of totals inclusion.
        Assert.Equal(1, summary.Validation.ValidRows);
        Assert.Equal(5, summary.Validation.WarningRows);
        Assert.Equal(5, summary.Validation.ErrorRows);

        // Row 6 (AAM, VatAmount 8100, V4 error) is excluded from totals entirely.
        Assert.DoesNotContain(summary.PerCategory, c => c.VatCode == "AAM");

        // Row 10 (EUR, V3) is a Warning but still excluded from totals per data-contract.md.
        // Rows 2, 7, 8 (all VatCode 27 / OUT) remain: row 7's declared (mismatched) VatAmount
        // and row 8's declared (mismatched) GrossAmount are used as-is, never recomputed.
        var standardOut = summary.PerCategory.Single(c => c.VatCode == "27" && c.Direction == Direction.Out);
        Assert.Equal(3, standardOut.RowCount);
        Assert.Equal(300000m, standardOut.TotalNet);
        Assert.Equal(79000m, standardOut.TotalVat); // 27000 + 25000 (declared, mismatched) + 27000
        Assert.Equal(382000m, standardOut.TotalGross); // 127000 + 125000 + 130000 (declared, mismatched)

        // Row 9 (bad tax number, V7 warning) is included as its own category.
        var reducedOut18 = summary.PerCategory.Single(c => c.VatCode == "18" && c.Direction == Direction.Out);
        Assert.Equal(1, reducedOut18.RowCount);
        Assert.Equal(60000m, reducedOut18.TotalNet);
        Assert.Equal(10800m, reducedOut18.TotalVat);

        // Row 11 (duplicate invoice number, V8 warning) is included as its own category.
        var reducedOut5 = summary.PerCategory.Single(c => c.VatCode == "5" && c.Direction == Direction.Out);
        Assert.Equal(1, reducedOut5.RowCount);
        Assert.Equal(40000m, reducedOut5.TotalNet);
        Assert.Equal(2000m, reducedOut5.TotalVat);

        Assert.Equal(3, summary.PerCategory.Count);
        Assert.Equal(91800m, summary.TotalOutputVat); // 79000 + 10800 + 2000
        Assert.Equal(0m, summary.TotalDeductibleInputVat);
        Assert.Equal(91800m, summary.NetVatPayable);

        Assert.Equal(DeclarationStatus.CompletedWithWarnings, DeclarationStatusCalculator.FromValidation(summary.Validation));
    }

    private static void AssertCategory(DeclarationSummary summary, string vatCode, Direction direction, int rowCount, decimal net, decimal vat, decimal gross)
    {
        var category = summary.PerCategory.Single(c => c.VatCode == vatCode && c.Direction == direction);
        Assert.Equal(rowCount, category.RowCount);
        Assert.Equal(net, category.TotalNet);
        Assert.Equal(vat, category.TotalVat);
        Assert.Equal(gross, category.TotalGross);
    }
}
