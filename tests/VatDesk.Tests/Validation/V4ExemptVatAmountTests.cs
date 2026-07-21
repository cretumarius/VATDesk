using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Tests.Validation;

public class V4ExemptVatAmountTests
{
    private static readonly VatCategory Aam = new("AAM", VatKind.Exempt, null, "Alanyi adómentes", "Personal exemption", 5);
    private static readonly VatCategory ZeroRated = new("0", VatKind.ZeroRated, 0m, "Nulla kulcs", "Zero-rated", 4);

    [Fact]
    public void ExemptCode_ZeroVatAmount_ReturnsNoIssue()
    {
        var line = TestLines.Valid(vatCode: "AAM", vatAmount: 0m, netAmount: 30000m, grossAmount: 30000m);

        var issue = TransactionLineValidator.CheckExemptVatAmountIsZero(line, Aam);

        Assert.Null(issue);
    }

    [Fact]
    public void ZeroRatedCode_ZeroVatAmount_ReturnsNoIssue()
    {
        var line = TestLines.Valid(vatCode: "0", vatAmount: 0m, netAmount: 120000m, grossAmount: 120000m);

        var issue = TransactionLineValidator.CheckExemptVatAmountIsZero(line, ZeroRated);

        Assert.Null(issue);
    }

    [Fact]
    public void ExemptCode_NonZeroVatAmount_ReturnsV4Error()
    {
        var line = TestLines.Valid(vatCode: "AAM", vatAmount: 8100m, netAmount: 30000m, grossAmount: 38100m, sourceRowNumber: 6);

        var issue = TransactionLineValidator.CheckExemptVatAmountIsZero(line, Aam);

        Assert.NotNull(issue);
        Assert.Equal(ValidationRuleIds.NonZeroVatOnExemptCode, issue!.RuleId);
        Assert.Equal(Severity.Error, issue.Severity);
        Assert.Equal(6, issue.RowNumber);
        Assert.Contains("8100", issue.Message);
    }
}
