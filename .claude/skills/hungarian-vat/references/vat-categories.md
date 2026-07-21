# VAT Category Registry

## Model

```csharp
enum VatKind { Percentage, ZeroRated, Exempt, ReverseCharge }

record VatCategory(
    string Code,            // stable identifier used in files, DB, API, UI
    VatKind Kind,
    decimal? Rate,          // only for Percentage/ZeroRated (0m for ZeroRated)
    string DisplayNameHu,
    string DisplayNameEn,
    int SortOrder);

interface IVatCategoryRegistry
{
    string CountryCode { get; }          // "HU", "RO", ...
    IReadOnlyList<VatCategory> All { get; }
    bool TryGet(string code, out VatCategory category);
}
```

The registry is the single source of truth for what codes exist. Parsers, validators,
aggregators, the API, the React UI, and the PDF renderer all consume it — never
hard-code category lists anywhere else. The API exposes it at
`GET /api/countries/{countryCode}/vat-categories` so the frontend renders categories
dynamically.

## Hungarian registry (`HungarianVatCategoryRegistry`, CountryCode = "HU")

| Code | Kind | Rate | Hungarian name | English name | Notes |
|---|---|---|---|---|---|
| `27` | Percentage | 0.27 | Általános kulcs (27%) | Standard rate (27%) | Standard rate; the EU's highest |
| `18` | Percentage | 0.18 | Kedvezményes kulcs (18%) | Reduced rate (18%) | Certain foodstuffs, event admission |
| `5` | Percentage | 0.05 | Kedvezményes kulcs (5%) | Reduced rate (5%) | Books, medicines, accommodation, etc. |
| `0` | ZeroRated | 0.00 | Nulla kulcs | Zero-rated | Intl./intra-EU transport etc.; VAT must be 0 |
| `AAM` | Exempt | — | Alanyi adómentes | Personal (subjective) exemption | Small-business exemption; VAT must be 0 |
| `TAM` | Exempt | — | Tárgyi adómentes | Exempt activity | Activity-based exemption (e.g. education, health); VAT must be 0 |
| `EUFAD` | ReverseCharge | — | EU-n belüli fordított adózás | Intra-EU reverse charge | Buyer accounts for VAT; VAT must be 0 |
| `FAD` | ReverseCharge | — | Belföldi fordított adózás | Domestic reverse charge | §142 domestic reverse charge; VAT must be 0 |

Validation behavior by kind:
- `Percentage`: expect `VatAmount ≈ NetAmount × Rate` (±1 HUF) → V5 warning on mismatch.
- `ZeroRated`, `Exempt`, `ReverseCharge`: `VatAmount` must be exactly 0 → V4 error otherwise.

Display order in reports: 27, 18, 5, 0, AAM, TAM, EUFAD, FAD (SortOrder ascending),
sales (OUT) block before purchases (IN) block.

## Extending to another country

Each country = one registry + one declaration strategy (see `architecture.md`). The
registry above is deliberately data-only so a new country is mostly a table like this
one. For Romania: create `RomanianVatCategoryRegistry` ("RO") — **verify current
Romanian rates at implementation time before encoding them** (they changed in
August 2025; do not trust memory, check an authoritative source and record it here).

Rules for any new registry:
- Codes must be stable, uppercase, ≤ 8 chars, unique within the country.
- Reuse `VatKind` semantics; if a country needs a genuinely new kind, extend the enum
  in Core and update validators centrally — never special-case in a parser.
- Add golden-value samples + expected totals for the new country alongside the
  Hungarian ones.
