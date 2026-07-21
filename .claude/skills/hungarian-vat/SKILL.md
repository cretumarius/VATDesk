---
name: hungarian-vat
description: Domain rules, data contracts, and architecture conventions for the VATDesk project — a Hungarian VAT (ÁFA) declaration generator built with ASP.NET Core, React/TypeScript, and PostgreSQL. Use this skill whenever working on the VATDesk repository or any task involving Hungarian VAT declarations, ÁFA bevallás, NAV Online Számla invoice data, VAT category codes (27, 18, 5, 0, AAM, TAM, EUFAD, FAD), invoice CSV/XML parsing, VAT calculation or validation logic, declaration report/PDF generation, or extending the system to new countries (e.g., Romanian VAT). Consult it even for small changes — field names, validation tolerances, and layer boundaries defined here must stay consistent across the codebase.
---

# Hungarian VAT Declaration Generator (VATDesk)

This skill encodes the domain knowledge and project conventions for VATDesk, a web app
that ingests invoice/transaction files (CSV or NAV 3.0-style XML), validates them,
aggregates amounts by Hungarian VAT category, and produces a declaration summary
(on-screen + PDF).

Consistency matters more than cleverness here: tax software lives and dies by exact
field names, exact category codes, and exact rounding rules. When in doubt, follow the
contracts in this skill rather than inventing a variation.

## Quick facts (always apply)

- Currency of the declaration: **HUF**. Non-HUF rows are not processed in v1 — flag them
  as warnings, never silently convert.
- Amounts are `decimal` (never float/double) in .NET; parse with `CultureInfo.InvariantCulture`.
- Rounding tolerance for validation checks: **±1 HUF** (covers legitimate rounding).
- Valid VAT codes: `27`, `18`, `5`, `0`, `AAM`, `TAM`, `EUFAD`, `FAD`. Anything else is a
  row-level **error**.
- Every record has a `Direction`: `OUT` (sales → output VAT) or `IN` (purchases →
  deductible input VAT). The declaration bottom line is
  `netVatPayable = totalOutputVat − totalDeductibleInputVat`.
- Files: max **5 MB**, `.csv` or `.xml`, format detected by **content sniffing**, not
  extension alone.
- XML must be parsed with `DtdProcessing.Prohibit` and `XmlResolver = null` (XXE defense).
  Never relax this.
- Partial-success model: one bad row rejects that row (with row number + reason), not the
  whole file. Only an unparseable file is a full failure.

## Reference files — read the one you need

- `references/data-contract.md` — the CSV and XML input contracts, field rules, and the
  full validation rule set with severities. Read before touching any parser, validator,
  or DTO.
- `references/vat-categories.md` — the Hungarian VAT category registry (rates, kinds,
  Hungarian/English display names, per-code validation behavior) and how the registry
  abstraction supports other countries. Read before touching calculation or category
  display logic.
- `references/architecture.md` — layer boundaries (Core / Infrastructure / Api), naming
  conventions, persistence schema, auth roles, and the "how to add a country" recipe.
  Read before adding files, moving logic between layers, or extending to a new country
  (e.g., Romania).

## Sample files — use these, don't invent new ones

- `assets/sample-clean.csv` — valid file covering every VAT code and both directions.
- `assets/sample-invalid.csv` — deliberately dirty file exercising each validation rule
  (use in tests and demos).
- `assets/sample-nav.xml` — simplified NAV 3.0-flavored XML input.

When writing tests or demos, copy these files rather than authoring new samples, so
expected totals stay stable across the project. The expected aggregate results for
`sample-clean.csv` are documented at the bottom of `references/data-contract.md` —
use them as golden values in unit tests.

## Non-negotiable behaviors

1. Never present a computed VAT amount as authoritative when it conflicts with the
   declared amount in the source file — surface both and flag the mismatch.
2. Exempt/reverse-charge codes (`AAM`, `TAM`, `EUFAD`, `FAD`, `0`) must have
   `VatAmount = 0`; a non-zero value is an **error**, not a warning.
3. UI and PDF show amounts with thousands separators and no decimals for HUF
   (e.g., `1 270 000 Ft` style in PDF, `1,270,000` in tables) — but persist and compute
   at full decimal precision.
4. Role gates: `Admin` may upload/generate; `Viewer` may only read and download PDFs.
   Enforce on the API (policy), not just in the UI.
5. This app produces a **declaration summary** for review purposes — it is not a filing
   with NAV and must never claim to be. Keep the disclaimer in the PDF footer.
