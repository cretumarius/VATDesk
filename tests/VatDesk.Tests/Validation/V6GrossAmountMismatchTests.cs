using VatDesk.Core.Validation;

namespace VatDesk.Tests.Validation;

public class V6GrossAmountMismatchTests
{
    [Fact]
    public void ExactMatch_ReturnsNoIssue()
    {
        var line = TestLines.Valid(netAmount: 100000m, vatAmount: 27000m, grossAmount: 127000m);

        var issue = TransactionLineValidator.CheckGrossAmountMatchesNetPlusVat(line);

        Assert.Null(issue);
    }

    [Fact]
    public void WithinOneHufTolerance_ReturnsNoIssue()
    {
        var line = TestLines.Valid(netAmount: 100000m, vatAmount: 27000m, grossAmount: 127001m);

        var issue = TransactionLineValidator.CheckGrossAmountMatchesNetPlusVat(line);

        Assert.Null(issue);
    }

    [Fact]
    public void OutsideTolerance_ReturnsV6Warning()
    {
        var line = TestLines.Valid(netAmount: 100000m, vatAmount: 27000m, grossAmount: 130000m, sourceRowNumber: 8);

        var issue = TransactionLineValidator.CheckGrossAmountMatchesNetPlusVat(line);

        Assert.NotNull(issue);
        Assert.Equal(ValidationRuleIds.GrossAmountMismatch, issue!.RuleId);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(8, issue.RowNumber);
        Assert.Contains("127000", issue.Message);
        Assert.Contains("130000", issue.Message);
    }
}
