using Microsoft.EntityFrameworkCore;
using VatDesk.Core.Models;
using VatDesk.Core.Validation;
using VatDesk.Infrastructure.Persistence;
using VatDesk.Infrastructure.Persistence.Repositories;

namespace VatDesk.Tests.Persistence;

public class DeclarationRepositoryTests
{
    private static VatDeskDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<VatDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VatDeskDbContext(options);
    }

    private static DeclarationSummary BuildSampleSummary() => new(
        PerCategory:
        [
            new CategoryTotal("27", Direction.Out, 1, 100000m, 27000m, 127000m),
        ],
        TotalOutputVat: 27000m,
        TotalDeductibleInputVat: 0m,
        NetVatPayable: 27000m,
        Validation: new ValidationSummary(
            ValidRows: 1,
            WarningRows: 0,
            ErrorRows: 0,
            Issues: []));

    [Fact]
    public async Task SaveAsync_PersistsDeclarationCategoryTotalsAndIssues_NoRawRows()
    {
        await using var db = NewDbContext();
        var repository = new DeclarationRepository(db);
        var summary = BuildSampleSummary();

        var saved = await repository.SaveAsync("invoices.csv", SourceFormat.Csv, "HU", summary);

        var reloaded = await repository.GetByIdAsync(saved.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("invoices.csv", reloaded!.SourceFilename);
        Assert.Equal(SourceFormat.Csv, reloaded.SourceFormat);
        Assert.Equal("HU", reloaded.CountryCode);
        Assert.Equal(DeclarationStatus.Completed, reloaded.Status);
        Assert.Equal(27000m, reloaded.TotalOutputVat);
        Assert.Null(reloaded.UserId);
        Assert.Single(reloaded.CategoryTotals);
        Assert.Equal("27", reloaded.CategoryTotals[0].VatCode);
        Assert.Empty(reloaded.ValidationIssues);
    }

    [Theory]
    [InlineData(5, 0, 0, DeclarationStatus.Completed)]
    [InlineData(8, 2, 0, DeclarationStatus.CompletedWithWarnings)]
    [InlineData(1, 0, 9, DeclarationStatus.CompletedWithWarnings)]
    [InlineData(0, 0, 10, DeclarationStatus.Failed)]
    public async Task SaveAsync_DerivesStatus_FromValidationCounts(int validRows, int warningRows, int errorRows, DeclarationStatus expected)
    {
        await using var db = NewDbContext();
        var repository = new DeclarationRepository(db);
        var summary = BuildSampleSummary() with
        {
            Validation = new ValidationSummary(validRows, warningRows, errorRows, []),
        };

        var saved = await repository.SaveAsync("invoices.csv", SourceFormat.Csv, "HU", summary);

        Assert.Equal(expected, saved.Status);
    }

    [Fact]
    public async Task ListAsync_ReturnsNewestFirst()
    {
        await using var db = NewDbContext();
        var repository = new DeclarationRepository(db);

        var first = await repository.SaveAsync("a.csv", SourceFormat.Csv, "HU", BuildSampleSummary());
        first.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await db.SaveChangesAsync();

        var second = await repository.SaveAsync("b.csv", SourceFormat.Csv, "HU", BuildSampleSummary());

        var list = await repository.ListAsync();

        Assert.Equal([second.Id, first.Id], list.Select(d => d.Id));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var db = NewDbContext();
        var repository = new DeclarationRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
