using VatDesk.Core.Validation;
using VatDesk.Infrastructure.Countries.Hu;

namespace VatDesk.Tests.Validation;

public class V2UnknownVatCodeTests
{
    private readonly HungarianVatCategoryRegistry _registry = new();

    [Theory]
    [InlineData("27")]
    [InlineData("AAM")]
    [InlineData("FAD")]
    public void KnownCode_ReturnsNoIssue_AndResolvesCategory(string code)
    {
        var line = TestLines.Valid(vatCode: code);

        var issue = TransactionLineValidator.CheckVatCodeKnown(line, _registry, out var category);

        Assert.Null(issue);
        Assert.NotNull(category);
        Assert.Equal(code, category!.Code);
    }

    [Fact]
    public void UnknownCode_ReturnsV2Error_AndNullCategory()
    {
        var line = TestLines.Valid(vatCode: "99", sourceRowNumber: 5);

        var issue = TransactionLineValidator.CheckVatCodeKnown(line, _registry, out var category);

        Assert.NotNull(issue);
        Assert.Null(category);
        Assert.Equal(ValidationRuleIds.UnknownVatCode, issue!.RuleId);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(5, issue.RowNumber);
        Assert.Contains("99", issue.Message);
    }
}
