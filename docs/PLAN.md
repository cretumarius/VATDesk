# VATDesk — Project Plan

Hungarian VAT (ÁFA) declaration generator. This document is the agreed implementation plan; domain
rules live in the project skill at `.claude/skills/hungarian-vat/`.

## Locked decisions

| Topic | Decision |
|---|---|
| Backend | ASP.NET Core 8 (LTS) Web API |
| Frontend | React 18 + TypeScript + Vite |
| Database | PostgreSQL, EF Core + Npgsql, migrations auto-applied on startup |
| PDF | QuestPDF (Community license) |
| Packaging | Multi-stage Dockerfile; docker-compose (app + postgres); single deployable unit (API serves built React app) |
| Deployment | Azure (Web App for Containers / Container Apps); Railway as fallback |
| Input formats | CSV (primary) + NAV 3.0-flavored XML; format by content sniffing |
| Declaration output | v1 = summary totals per VAT category, split OUT/IN, with net VAT payable (NOT the official 2465A form line structure) |
| Persistence | Aggregates + validation issues only; raw invoice rows NOT stored (PII tradeoff, documented) |
| Auth (baseline) | Seeded users + JWT: admin@demo.hu (Admin), viewer@demo.hu (Viewer) |
| Auth (stretch) | Microsoft Entra ID external login — only if time allows, must not block demo |
| Authorization | Admin: upload/generate; Viewer: view + PDF only. Enforced server-side |
| Extensibility | Country = IVatCategoryRegistry + IVatDeclarationStrategy per country code; Romania is the reference future case (verify RO rates at implementation time) |

## Phases & status

- [x] Phase 0 — Decisions & architecture (this plan)
- [x] Phase 0.5 — Custom Claude skill `hungarian-vat` created and committed
- [x] Phase 1 — UI design via Claude Design, found in `docs/design`
- [x] Phase 2 — Data contract + sample files (locked; see skill `references/data-contract.md`; samples in skill `assets/`)
- [x] Phase 3 — Backend: Core models → parsers → validation (V1–V8) → HU strategy → PDF → API endpoints
- [ ] Phase 4 — Frontend: login, dashboard/history, upload, report view, PDF download, role-aware UI
  - [x] 4.1 — Auth infrastructure + `/login` + app shell + dashboard empty state
  - [x] 4.2 — Upload flow (drag/drop, sample files, processing states)
  - [x] 4.3 — Report view (summary cards, validation panel, category breakdown)
  - [ ] 4.4 — Dashboard/history (declarations table, filters, PDF download from list)
- [x] Phase 5 — Auth & authorization baseline (JWT + roles) — done, folded into Phase 4.1
  - [ ] Entra ID external login (stretch — only if time allows, must not block demo)
- [ ] Phase 6 — Security hardening (11-point checklist in skill `references/architecture.md`)
- [ ] Phase 7 — Packaging: Dockerfile/compose, Azure deploy, README, AI conversation log export

## Deliverables checklist (from the challenge)

- [ ] Running deployment (Azure URL) reviewers can test instantly
- [ ] GitHub repository URL
- [ ] AI conversation log (planning session + Claude Code sessions), unedited
- [ ] README: run instructions, sample files, demo credentials, architecture diagram,
      "how to add a country" section, AI-assisted workflow section (skill + MCPs)

## AI toolchain story (for submission notes)

1. Planning session in Claude (this log): requirements analysis, domain research
   (NAV 3.0 XML, HU VAT categories), architecture, data contract.
2. Custom project skill `.claude/skills/hungarian-vat/` encodes domain rules, contracts,
   conventions, and canonical sample files for all subsequent Claude Code sessions.
3. Claude Design: UI generated from written design prompt; synced into implementation
   via Claude Design MCP.
4. GitHub MCP: repo/commits/PRs managed by Claude Code.
5. Postgres MCP: live schema/data inspection during debugging.

## Scope guardrails

- v1 processes HUF only; non-HUF rows are warnings.
- The app produces a review summary, not an official NAV filing (disclaimer in PDF).

## Key API surface (details in skill references/architecture.md)

POST /api/auth/login · GET /api/countries/{cc}/vat-categories ·
POST /api/declarations (Admin) · GET /api/declarations ·
GET /api/declarations/{id} · GET /api/declarations/{id}/pdf ·
GET /api/samples/{clean.csv,invalid.csv,nav.xml} (any authenticated user)

## Design gaps (accumulated across sessions)

- **Dark mode** (4.1): not in the design (light-mode only); derived by inverting
  neutrals, brightening the teal accent, lightening status colors for contrast.
- **Demo credential copy** (4.1): mockup was a client-only fake login ("any password
  works"); ours checks a real password, so the demo buttons autofill the real one.
- **`UserEntity.DisplayName`** (4.1): not in architecture.md's original `users` schema;
  added (additive) for the JWT display-name claim and the header/account-menu UI.
- **Format-hint tabs** (4.2): design shows illustrative CSV/XML structure that conflicts
  with data-contract.md's locked schema (wrong column names, `direction=sales|purchase`
  instead of `OUT|IN`, a fake XML `invoiceMain`/`lineVatRate`). Skill wins — tabs show
  the real contract.
- **Dropzone's third quick-fill button** (4.2): design has "Try a broken file" (demos a
  structural parse failure); no canonical corrupt-file skill asset exists, so it's
  "Use sample with warnings" (real sample-invalid.csv) instead, demonstrating
  CompletedWithWarnings with real data.
- **Money formatting: comma vs. space grouping** (4.3): the design mockup's own fake
  `fmt()` helper uses comma/en-US-style grouping ("94,300"). The skill's explicit hard
  rule #3 and this session's prompt both specify `Intl.NumberFormat('hu-HU')`, which
  groups with a (non-breaking) space ("94 300") and — per real Hungarian CLDR data —
  does not group 4-digit numbers at all (e.g. `2500`, not `2 500`). Skill wins; the
  report renders hu-HU formatting throughout, not the mockup's comma style.
- **Report header "Period" field** (4.3): the design shows a hardcoded "Period · 2025 Q2
  (Apr–Jun)" line. There is no period/quarter concept anywhere in the data model
  (`DeclarationDto` has no date range), so it was dropped rather than fabricated. Open
  question below for whether to add one.
- **Category display name vs. registry text** (4.3): design shows "Exempt (subjective)"
  for AAM; the registry's canonical `DisplayNameEn` is "Personal (subjective)
  exemption". Skill wins — the report renders the registry text verbatim (a
  presentational regex only strips a redundant trailing "(NN%)" already shown in the
  adjacent code badge, e.g. "Standard rate (27%)" → "Standard rate").
- **Negative Net VAT Payable (reclaimable)** (4.3): the design mockup only demonstrates
  the positive/payable case. The report keeps the same teal card treatment for a
  negative value — the minus sign comes naturally from `Intl.NumberFormat`, and the
  caption swaps to "reclaimable from NAV". Not yet exercised by a real sample file.

## Open questions for a future session

- Should a period/quarter field be added (backend-derived from `CreatedAt`, or a new
  input at upload time), or is the report meant to just be "as of processing date"
  with no period concept? See the "Period" design gap above.

## What Phase 4.4 needs

- `GET /api/declarations` already returns `DeclarationListItemDto[]` (id, filename,
  format, country, status, the three VAT totals, valid/warning/error row counts,
  createdAt) — no backend changes anticipated for a basic table.
- `declarationStatusLabel`/`declarationStatusBadgeVariant` (`lib/declaration-status.ts`)
  and `formatAmount`/`formatDate` (`lib/format.ts`) are already shared and ready to
  reuse in a list/table view.
- `downloadDeclarationPdf(id)` (`api/client.ts`) already streams+saves a PDF — reusable
  for a per-row download action in the history table without new client code.
- The dashboard's empty state (`DashboardPage.tsx`) still always renders regardless of
  whether declarations exist; 4.4 is what replaces it with the real table + wires
  `GET /api/declarations`.

## Shared component inventory (`frontend/src/components/ui/`)

`alert`, `avatar`, `badge`, `button`, `card`, `dropdown-menu`, `input`, `label`, `tabs`,
`theme-toggle`, `toast`, `tooltip` — plus feature components in `components/upload/`
(DropZone, FileCard, FormatHintTabs, ProcessingScreen, UploadErrorScreen) and
`components/report/` (ReportHeader, SummaryCards, ValidationPanel,
CategoryBreakdownTable, ReportSkeleton). No new primitives were needed this session —
the report view composed entirely from Card/Badge/Button plus new feature-specific
components, per the "extend, never fork" rule.
