# VATDesk Input Data Contract

Two input formats are supported. Format is decided by content sniffing: a file whose
first non-whitespace character is `<` is treated as XML; otherwise CSV is attempted.
Extension is a hint, never the decider.

## 1. CSV contract

- Encoding: UTF-8 (accept BOM). Delimiter: comma. Header row **required**.
- Decimal separator: `.` — parse with invariant culture. Reject `,` decimals as a
  row error with a helpful message ("use 27000.50, not 27000,50").
- Dates: ISO `yyyy-MM-dd` only.
- Header matching: case-insensitive, order-independent. Unknown extra columns are
  ignored with a file-level info notice.

### Columns

| Column | Required | Rules |
|---|---|---|
| `InvoiceNumber` | yes | non-empty, ≤ 50 chars, trimmed |
| `IssueDate` | yes | `yyyy-MM-dd`, not in the future by more than 1 day |
| `PartnerName` | no | ≤ 200 chars |
| `PartnerTaxNumber` | no | if present: Hungarian format `8d-1d-2d` (e.g. `12345678-2-41`) or EU VAT id (`[A-Z]{2}[A-Z0-9]{2,12}`); mismatch → warning |
| `Direction` | no | `OUT` or `IN` (case-insensitive). Missing column ⇒ all rows `OUT` + file-level notice |
| `NetAmount` | yes | decimal ≥ 0 |
| `VatCode` | yes | one of `27`, `18`, `5`, `0`, `AAM`, `TAM`, `EUFAD`, `FAD` |
| `VatAmount` | yes | decimal ≥ 0 |
| `GrossAmount` | yes | decimal ≥ 0 |
| `Currency` | no | ISO 4217; missing ⇒ `HUF`. Non-`HUF` ⇒ row skipped with warning |

### Canonical sample row

```csv
InvoiceNumber,IssueDate,PartnerName,PartnerTaxNumber,Direction,NetAmount,VatCode,VatAmount,GrossAmount,Currency
INV-2026-001,2026-06-03,Kovács Kft.,12345678-2-41,OUT,100000,27,27000,127000,HUF
```

## 2. XML contract (NAV 3.0-flavored, simplified)

Namespace: `http://schemas.nav.gov.hu/OSA/3.0/data`. One `<InvoiceData>` root per file
(v1). Element names deliberately mirror the real NAV Online Számla 3.0 schema subset.

```xml
<InvoiceData xmlns="http://schemas.nav.gov.hu/OSA/3.0/data">
  <invoiceNumber>INV-2026-001</invoiceNumber>
  <invoiceIssueDate>2026-06-03</invoiceIssueDate>
  <supplierTaxNumber>12345678-2-41</supplierTaxNumber>
  <customerName>Kovács Kft.</customerName>
  <invoiceDirection>OUTBOUND</invoiceDirection> <!-- OUTBOUND→OUT, INBOUND→IN; optional, default OUTBOUND -->
  <invoiceLines>
    <line>
      <lineNetAmount>100000</lineNetAmount>
      <lineVatData>
        <!-- exactly one of vatPercentage | vatExemption per line -->
        <vatPercentage>0.27</vatPercentage>
        <lineVatAmount>27000</lineVatAmount>
      </lineVatData>
      <lineGrossAmount>127000</lineGrossAmount>
    </line>
  </invoiceLines>
</InvoiceData>
```

- `vatPercentage` values map to codes: `0.27`→`27`, `0.18`→`18`, `0.05`→`5`, `0`→`0`.
  Any other percentage → line error.
- Exemption form: `<vatExemption case="AAM"/>` (case ∈ `AAM`, `TAM`, `EUFAD`, `FAD`);
  implies `lineVatAmount` must be 0 or absent.
- Each `<line>` becomes one internal record; `InvoiceNumber`/date/partner are inherited
  from the invoice header.
- **Parser hardening (mandatory):** `XmlReaderSettings` with
  `DtdProcessing = DtdProcessing.Prohibit`, `XmlResolver = null`,
  `MaxCharactersInDocument` capped. Reject files > 5 MB before parsing.

## 3. Internal canonical record

Both parsers normalize into this shape (Core layer):

```csharp
record TransactionLine(
    string InvoiceNumber,
    DateOnly IssueDate,
    string? PartnerName,
    string? PartnerTaxNumber,
    Direction Direction,        // Out | In
    decimal NetAmount,
    string VatCode,
    decimal VatAmount,
    decimal GrossAmount,
    string Currency,
    int SourceRowNumber);       // 1-based, for error reporting
```

## 4. Validation rules (run in this order per row)

| # | Rule | Severity |
|---|---|---|
| V1 | Required fields present and well-formed | Error |
| V2 | `VatCode` in the registry | Error |
| V3 | Currency is HUF | Warning (row skipped from totals) |
| V4 | Exempt/RC/zero codes ⇒ `VatAmount == 0` | Error |
| V5 | Percentage codes ⇒ `VatAmount ≈ NetAmount × rate` within ±1 HUF | Warning (declared value still used in totals) |
| V6 | `NetAmount + VatAmount ≈ GrossAmount` within ±1 HUF | Warning (declared values still used) |
| V7 | `PartnerTaxNumber` format | Warning |
| V8 | Duplicate `InvoiceNumber` + `Direction` within file | Warning |

Principles:
- Rows with **errors** are excluded from totals; rows with only **warnings** are included.
- Every issue carries: row number, severity, rule id, human message (English, concise,
  includes expected vs actual values).
- Warnings on V5/V6 use the **declared** amounts in totals (the source file is the legal
  document; we flag, we do not correct).

## 5. Aggregation output

```
DeclarationSummary
├── perCategory[]: { vatCode, direction, rowCount, totalNet, totalVat, totalGross }
├── totalOutputVat        (Σ VatAmount where Direction=OUT, error-free rows)
├── totalDeductibleInputVat (Σ VatAmount where Direction=IN, error-free rows)
├── netVatPayable         (output − input; may be negative = reclaimable)
└── validation: { validRows, warningRows, errorRows, issues[] }
```

## 6. Golden values for assets/sample-clean.csv

The clean sample contains 10 rows: 7 OUT, 3 IN. Expected results (use in unit tests):

| VatCode | Direction | Rows | Net | VAT | Gross |
|---|---|---|---|---|---|
| 27 | OUT | 2 | 300,000 | 81,000 | 381,000 |
| 18 | OUT | 1 | 60,000 | 10,800 | 70,800 |
| 5 | OUT | 1 | 50,000 | 2,500 | 52,500 |
| 0 | OUT | 1 | 120,000 | 0 | 120,000 |
| AAM | OUT | 1 | 30,000 | 0 | 30,000 |
| EUFAD | OUT | 1 | 200,000 | 0 | 200,000 |
| 27 | IN | 2 | 180,000 | 48,600 | 228,600 |
| 5 | IN | 1 | 40,000 | 2,000 | 42,000 |

- totalOutputVat = **94,300**
- totalDeductibleInputVat = **50,600**
- netVatPayable = **43,700**
- validRows = 10, warnings = 0, errors = 0
