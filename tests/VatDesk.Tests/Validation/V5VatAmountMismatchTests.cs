using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Tests.Validation;

public class V5VatAmountMismatchTests
{
    private static readonly VatCategory Standard27 = new("27", VatKind.Percentage, 0.27m, "Általános kulcs (27%)", "Standard rate (27%)", 1);

    [Fact]
    public void ExactMatch_ReturnsNoIssue()
    {
        var line = TestLines.Valid(netAmount: 100000m, vatCode: "27", vatAmount: 27000m);

        var issue = TransactionLineValidator.CheckVatAmountMatchesRate(line, Standard27);

        Assert.Null(issue);
    }

    [Fact]
    public void WithinOneHufTolerance_ReturnsNoIssue()
    {
        var line = TestLines.Valid(netAmount: 100000m, vatCode: "27", vatAmount: 27001m);

        var issue = TransactionLineValidator.CheckVatAmountMatchesRate(line, Standard27);

        Assert.Null(issue);
    }

    [Fact]
    public void OutsideTolerance_ReturnsV5Warning()
    {
        var line = TestLines.Valid(netAmount: 100000m, vatCode: "27", vatAmount: 25000m, sourceRowNumber: 7);

        var issue = TransactionLineValidator.CheckVatAmountMatchesRate(line, Standard27);

        Assert.NotNull(issue);
        Assert.Equal(ValidationRuleIds.VatAmountMismatch, issue!.RuleId);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(7, issue.RowNumber);
        Assert.Contains("25000", issue.Message);
        Assert.Contains("27000", issue.Message);
    }
}
