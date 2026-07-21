using VatDesk.Core.Validation;

namespace VatDesk.Tests.Validation;

public class V7PartnerTaxNumberTests
{
    [Fact]
    public void MissingTaxNumber_ReturnsNoIssue()
    {
        var line = TestLines.Valid(partnerTaxNumber: null);

        var issue = TransactionLineValidator.CheckPartnerTaxNumberFormat(line);

        Assert.Null(issue);
    }

    [Fact]
    public void ValidHungarianFormat_ReturnsNoIssue()
    {
        var line = TestLines.Valid(partnerTaxNumber: "12345678-2-41");

        var issue = TransactionLineValidator.CheckPartnerTaxNumberFormat(line);

        Assert.Null(issue);
    }

    [Fact]
    public void ValidEuVatId_ReturnsNoIssue()
    {
        var line = TestLines.Valid(partnerTaxNumber: "DE123456789");

        var issue = TransactionLineValidator.CheckPartnerTaxNumberFormat(line);

        Assert.Null(issue);
    }

    [Fact]
    public void MalformedTaxNumber_ReturnsV7Warning()
    {
        var line = TestLines.Valid(partnerTaxNumber: "NOT-A-TAX-NO", sourceRowNumber: 9);

        var issue = TransactionLineValidator.CheckPartnerTaxNumberFormat(line);

        Assert.NotNull(issue);
        Assert.Equal(ValidationRuleIds.PartnerTaxNumberFormat, issue!.RuleId);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(9, issue.RowNumber);
        Assert.Contains("NOT-A-TAX-NO", issue.Message);
    }
}
