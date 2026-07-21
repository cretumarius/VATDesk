using System.Text;
using QuestPDF.Infrastructure;
using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;
using VatDesk.Core.Validation;
using VatDesk.Infrastructure.Pdf;

namespace VatDesk.Tests.Pdf;

public class QuestPdfReportRendererTests
{
    static QuestPdfReportRendererTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public async Task RenderAsync_ProducesNonEmptyValidPdf()
    {
        var renderer = new QuestPdfReportRenderer();
        var summary = new DeclarationSummary(
            PerCategory: [new CategoryTotal("27", Direction.Out, 1, 100000m, 27000m, 127000m)],
            TotalOutputVat: 27000m,
            TotalDeductibleInputVat: 0m,
            NetVatPayable: 27000m,
            Validation: new ValidationSummary(1, 0, 0, [new ValidationIssue(0, ValidationRuleIds.FileNotice, Severity.Info, "info")]));
        var metadata = new DeclarationMetadata(Guid.NewGuid(), "sample-clean.csv", DateTimeOffset.UtcNow);

        var bytes = await renderer.RenderAsync(summary, metadata);

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task RenderAsync_EmptyCategoryList_StillProducesValidPdf()
    {
        var renderer = new QuestPdfReportRenderer();
        var summary = new DeclarationSummary([], 0m, 0m, 0m, new ValidationSummary(0, 0, 0, []));
        var metadata = new DeclarationMetadata(Guid.NewGuid(), "empty.csv", DateTimeOffset.UtcNow);

        var bytes = await renderer.RenderAsync(summary, metadata);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
