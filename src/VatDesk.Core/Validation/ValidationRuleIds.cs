namespace VatDesk.Core.Validation;

/// <summary>Stable rule ids referenced by ValidationIssue.RuleId; see data-contract.md section 4.</summary>
public static class ValidationRuleIds
{
    public const string RequiredFields = "V1";
    public const string UnknownVatCode = "V2";
    public const string NonHufCurrency = "V3";
    public const string NonZeroVatOnExemptCode = "V4";
    public const string VatAmountMismatch = "V5";
    public const string GrossAmountMismatch = "V6";
    public const string PartnerTaxNumberFormat = "V7";
    public const string DuplicateInvoiceNumber = "V8";
}
