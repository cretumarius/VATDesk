namespace VatDesk.Core.Models;

/// <summary>Single source of truth for a country's VAT codes; never hard-code category lists elsewhere.</summary>
public record VatCategory(
    string Code,
    VatKind Kind,
    decimal? Rate,
    string DisplayNameHu,
    string DisplayNameEn,
    int SortOrder);
