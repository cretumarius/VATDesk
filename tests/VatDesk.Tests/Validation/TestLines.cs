using VatDesk.Core.Models;

namespace VatDesk.Tests.Validation;

internal static class TestLines
{
    public static TransactionLine Valid(
        string invoiceNumber = "INV-1",
        DateOnly? issueDate = null,
        string? partnerName = "Partner Kft.",
        string? partnerTaxNumber = "12345678-2-41",
        Direction direction = Direction.Out,
        decimal netAmount = 100000m,
        string vatCode = "27",
        decimal vatAmount = 27000m,
        decimal grossAmount = 127000m,
        string currency = "HUF",
        int sourceRowNumber = 2) =>
        new(
            invoiceNumber,
            issueDate ?? new DateOnly(2026, 6, 3),
            partnerName,
            partnerTaxNumber,
            direction,
            netAmount,
            vatCode,
            vatAmount,
            grossAmount,
            currency,
            sourceRowNumber);
}
