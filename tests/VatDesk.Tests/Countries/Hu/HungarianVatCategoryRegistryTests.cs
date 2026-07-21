using VatDesk.Core.Models;
using VatDesk.Infrastructure.Countries.Hu;

namespace VatDesk.Tests.Countries.Hu;

public class HungarianVatCategoryRegistryTests
{
    private readonly HungarianVatCategoryRegistry _registry = new();

    [Fact]
    public void CountryCode_IsHu()
    {
        Assert.Equal("HU", _registry.CountryCode);
    }

    [Fact]
    public void All_HasExactlyEightCodes()
    {
        Assert.Equal(8, _registry.All.Count);
    }

    [Theory]
    [InlineData("27", VatKind.Percentage, 0.27, 1)]
    [InlineData("18", VatKind.Percentage, 0.18, 2)]
    [InlineData("5", VatKind.Percentage, 0.05, 3)]
    [InlineData("0", VatKind.ZeroRated, 0.00, 4)]
    public void PercentageAndZeroRatedCodes_HaveExpectedRateAndSortOrder(string code, VatKind kind, double rate, int sortOrder)
    {
        Assert.True(_registry.TryGet(code, out var category));
        Assert.Equal(kind, category.Kind);
        Assert.Equal((decimal)rate, category.Rate);
        Assert.Equal(sortOrder, category.SortOrder);
    }

    [Theory]
    [InlineData("AAM", VatKind.Exempt, 5)]
    [InlineData("TAM", VatKind.Exempt, 6)]
    [InlineData("EUFAD", VatKind.ReverseCharge, 7)]
    [InlineData("FAD", VatKind.ReverseCharge, 8)]
    public void ExemptAndReverseChargeCodes_HaveNullRate(string code, VatKind kind, int sortOrder)
    {
        Assert.True(_registry.TryGet(code, out var category));
        Assert.Equal(kind, category.Kind);
        Assert.Null(category.Rate);
        Assert.Equal(sortOrder, category.SortOrder);
    }

    [Fact]
    public void TryGet_UnknownCode_ReturnsFalse()
    {
        Assert.False(_registry.TryGet("99", out _));
    }

    [Fact]
    public void All_IsOrderedByDisplaySortOrder_27_18_5_0_AAM_TAM_EUFAD_FAD()
    {
        var codes = _registry.All.OrderBy(c => c.SortOrder).Select(c => c.Code);
        Assert.Equal(["27", "18", "5", "0", "AAM", "TAM", "EUFAD", "FAD"], codes);
    }
}
