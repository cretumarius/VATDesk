using VatDesk.Core.Abstractions;
using VatDesk.Core.Models;

namespace VatDesk.Infrastructure.Countries.Hu;

/// <summary>Hungarian VAT category registry; see hungarian-vat skill references/vat-categories.md.</summary>
public class HungarianVatCategoryRegistry : IVatCategoryRegistry
{
    public string CountryCode => "HU";

    public IReadOnlyList<VatCategory> All { get; } =
    [
        new("27", VatKind.Percentage, 0.27m, "Általános kulcs (27%)", "Standard rate (27%)", 1),
        new("18", VatKind.Percentage, 0.18m, "Kedvezményes kulcs (18%)", "Reduced rate (18%)", 2),
        new("5", VatKind.Percentage, 0.05m, "Kedvezményes kulcs (5%)", "Reduced rate (5%)", 3),
        new("0", VatKind.ZeroRated, 0.00m, "Nulla kulcs", "Zero-rated", 4),
        new("AAM", VatKind.Exempt, null, "Alanyi adómentes", "Personal (subjective) exemption", 5),
        new("TAM", VatKind.Exempt, null, "Tárgyi adómentes", "Exempt activity", 6),
        new("EUFAD", VatKind.ReverseCharge, null, "EU-n belüli fordított adózás", "Intra-EU reverse charge", 7),
        new("FAD", VatKind.ReverseCharge, null, "Belföldi fordított adózás", "Domestic reverse charge", 8),
    ];

    public bool TryGet(string code, out VatCategory category)
    {
        var match = All.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.Ordinal));
        category = match!;
        return match is not null;
    }
}
