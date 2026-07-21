using VatDesk.Core.Validation;

namespace VatDesk.Tests.Validation;

public class V3CurrencyTests
{
    [Theory]
    [InlineData("HUF")]
    [InlineData("huf")]
    public void HufCurrency_CaseInsensitive_ReturnsNoIssue(string currency)
    {
        var line = TestLines.Valid(currency: currency);

        var issue = TransactionLineValidator.CheckCurrencyIsHuf(line);

        Assert.Null(issue);
    }

    [Fact]
    public void NonHufCurrency_ReturnsV3Warning()
    {
        var line = TestLines.Valid(currency: "EUR", sourceRowNumber: 10);

        var issue = TransactionLineValidator.CheckCurrencyIsHuf(line);

        Assert.NotNull(issue);
        Assert.Equal(ValidationRuleIds.NonHufCurrency, issue!.RuleId);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(10, issue.RowNumber);
        Assert.Contains("EUR", issue.Message);
    }
}
