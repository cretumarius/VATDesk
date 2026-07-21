using VatDesk.Core.Models;

namespace VatDesk.Core.Abstractions;

/// <summary>Single source of truth for a country's VAT codes. Never hard-code category lists elsewhere.</summary>
public interface IVatCategoryRegistry
{
    string CountryCode { get; }

    IReadOnlyList<VatCategory> All { get; }

    bool TryGet(string code, out VatCategory category);
}
