using VatDesk.Core.Models;
using VatDesk.Core.Validation;

namespace VatDesk.Tests.Validation;

public class V8DuplicateInvoiceNumberTests
{
    [Fact]
    public void FirstOccurrence_ReturnsNoIssue()
    {
        var seen = new HashSet<(string, Direction)>();
        var line = TestLines.Valid(invoiceNumber: "INV-1", direction: Direction.Out);

        var issue = TransactionLineValidator.CheckDuplicateInvoiceNumber(line, seen);

        Assert.Null(issue);
        Assert.Contains(("INV-1", Direction.Out), seen);
    }

    [Fact]
    public void SecondOccurrence_SameInvoiceAndDirection_ReturnsV8Warning()
    {
        var seen = new HashSet<(string, Direction)>();
        var first = TestLines.Valid(invoiceNumber: "INV-1", direction: Direction.Out, sourceRowNumber: 2);
        var second = TestLines.Valid(invoiceNumber: "INV-1", direction: Direction.Out, sourceRowNumber: 11);

        TransactionLineValidator.CheckDuplicateInvoiceNumber(first, seen);
        var issue = TransactionLineValidator.CheckDuplicateInvoiceNumber(second, seen);

        Assert.NotNull(issue);
        Assert.Equal(ValidationRuleIds.DuplicateInvoiceNumber, issue!.RuleId);
        Assert.Equal(Severity.Warning, issue.Severity);
        Assert.Equal(11, issue.RowNumber);
        Assert.Contains("INV-1", issue.Message);
    }

    [Fact]
    public void SameInvoiceNumber_DifferentDirection_ReturnsNoIssue()
    {
        var seen = new HashSet<(string, Direction)>();
        var outLine = TestLines.Valid(invoiceNumber: "INV-1", direction: Direction.Out);
        var inLine = TestLines.Valid(invoiceNumber: "INV-1", direction: Direction.In);

        TransactionLineValidator.CheckDuplicateInvoiceNumber(outLine, seen);
        var issue = TransactionLineValidator.CheckDuplicateInvoiceNumber(inLine, seen);

        Assert.Null(issue);
    }
}
