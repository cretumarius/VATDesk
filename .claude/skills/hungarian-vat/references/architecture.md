# VATDesk Architecture & Conventions

## Stack

- Backend: ASP.NET Core 8 (LTS) Web API, EF Core + Npgsql, QuestPDF (Community license).
- Frontend: React 18 + TypeScript + Vite, served as static files by the API in
  production (single deployable unit).
- DB: PostgreSQL. Migrations applied automatically on startup.
- Packaging: multi-stage Dockerfile (node build → dotnet build → runtime image);
  docker-compose adds Postgres. Deployment target: Azure (Web App for Containers or
  Container Apps); Railway is the fallback.

## Layering (enforced dependency direction: Api → Infrastructure → Core)

```
src/
├── VatDesk.Core/            # zero external dependencies
│   ├── Models/              # TransactionLine, DeclarationSummary, VatCategory, ...
│   ├── Abstractions/        # IInvoiceParser, IVatCategoryRegistry,
│   │                        # IVatDeclarationStrategy, IReportRenderer
│   └── Validation/          # rule ids V1–V8, ValidationIssue, severities
├── VatDesk.Infrastructure/
│   ├── Parsing/             # CsvInvoiceParser, NavXmlInvoiceParser, ParserFactory
│   ├── Countries/Hu/        # HungarianVatCategoryRegistry, HungarianVatDeclarationStrategy
│   ├── Pdf/                 # QuestPdfReportRenderer
│   └── Persistence/         # DbContext, entities, repositories, migrations
└── VatDesk.Api/
    ├── Controllers/         # thin: validate → delegate → map to DTO
    ├── Auth/                # JWT issuance, role policies
    └── Dtos/
frontend/                    # React app (Vite)
tests/
└── VatDesk.Tests/           # xUnit; golden-value tests use skill assets
```

Rules:
- Core never references EF, ASP.NET, QuestPDF, or any parser library.
- Strategies and registries are registered per country code in DI
  (`services.AddCountry<HungarianVatCategoryRegistry, HungarianVatDeclarationStrategy>("HU")`),
  resolved via a small `ICountryResolver`. v1 hard-defaults to "HU" but the seam exists.
- Controllers contain no business logic. Aggregation math lives only in the strategy.
- All money math in `decimal`. Any `double` touching an amount is a bug.

## API surface (v1)

| Method | Route | Roles | Purpose |
|---|---|---|---|
| POST | `/api/auth/login` | anonymous | JWT for seeded users |
| GET | `/api/countries/{cc}/vat-categories` | any authenticated | registry for UI |
| POST | `/api/declarations` | Admin | multipart upload → process → persist → summary DTO |
| GET | `/api/declarations` | Admin, Viewer | history list |
| GET | `/api/declarations/{id}` | Admin, Viewer | full summary + validation issues |
| GET | `/api/declarations/{id}/pdf` | Admin, Viewer | PDF stream |

Errors: RFC 7807 ProblemDetails everywhere; global exception handler; no stack traces
to clients; validation problems return 400 with field detail.

## Persistence schema

- `users` (id uuid, email unique, password_hash, role text check in ('Admin','Viewer'), created_at)
- `declarations` (id uuid, user_id fk, source_filename, source_format text check in
  ('Csv','NavXml'), country_code, status text check in
  ('Completed','CompletedWithWarnings','Failed'), total_output_vat numeric,
  total_input_vat numeric, net_vat_payable numeric, valid_rows int, warning_rows int,
  error_rows int, created_at timestamptz)
- `declaration_category_totals` (declaration_id fk, vat_code, direction text check in
  ('Out','In'), row_count int, total_net numeric, total_vat numeric, total_gross numeric,
  primary key (declaration_id, vat_code, direction))
- `validation_issues` (id bigserial, declaration_id fk, row_number int, rule_id text,
  severity text check in ('Info','Warning','Error'), message text)

Deliberate v1 tradeoff (document in README): raw invoice rows are **not** persisted —
smaller PII surface, simpler schema. Only aggregates + issues are stored.

Seeded users (README + login hint box): `admin@demo.hu` / `Admin123!` (Admin),
`viewer@demo.hu` / `Viewer123!` (Viewer). Passwords hashed (ASP.NET Identity hasher or
BCrypt); never store plaintext even for demo users.

## Security checklist (implement all; each is a README talking point)

1. Upload: 5 MB limit (Kestrel + explicit check), extension allow-list, content
   sniffing, empty-file rejection.
2. XML: `DtdProcessing.Prohibit`, `XmlResolver = null`, char cap — XXE/entity-expansion
   defense.
3. Parsing: invariant culture, `decimal` only, no `Eval`-style dynamic anything.
4. AuthN: JWT (HS256, key from env/user-secrets — never committed), short expiry.
5. AuthZ: role policies on endpoints (server-side), UI gating is cosmetic only.
6. Rate limiting: ASP.NET `AddRateLimiter`, tight policy on `/api/auth/login` and
   uploads.
7. CORS: locked to the app origin (irrelevant in single-origin prod, still configured).
8. Headers: HSTS, X-Content-Type-Options, X-Frame-Options DENY, minimal CSP.
9. Errors: ProblemDetails only; log details server-side (Serilog), generic message out.
10. CSV output (if ever exporting): prefix `=`, `+`, `-`, `@` cells with `'` —
    formula-injection defense.
11. Secrets: connection strings + JWT key via env vars; `.env` git-ignored;
    docker-compose uses env, Azure uses App Settings.

## How to add a country (the extensibility recipe — keep this working)

1. `Infrastructure/Countries/Xx/` → `XxVatCategoryRegistry` (verify current official
   rates first) + `XxVatDeclarationStrategy`.
2. Register in DI with country code.
3. Add sample files + golden values (skill assets + tests).
4. No changes should be needed in Core, controllers, parsers, or the React components —
   if a change is needed there, the abstraction leaked; fix the abstraction instead.

## Frontend conventions

- Types in `frontend/src/api/types.ts` mirror API DTOs 1:1 (same names, camelCase).
- Amounts formatted with `Intl.NumberFormat('hu-HU')`; never format server-side for UI.
- Role from JWT claims drives visibility (upload button etc.) — cosmetic layer over
  server enforcement.
- States to implement for every async view: loading, empty, error, success — the
  Claude Design mockups define their appearance; follow them.

## PDF (QuestPDF)

- A4, header with app name + declaration id + generated-at timestamp, summary cards,
  category table (grouped OUT then IN), validation summary, footer disclaimer:
  "Generated by VATDesk for review purposes. This document is not an official filing
  with NAV." Include page numbers.
