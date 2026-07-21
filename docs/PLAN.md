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
- [x] Phase 6 — Security hardening (11-point checklist in skill `references/architecture.md`)
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

## Phase 6 summary (security audit + hardening)

Full verdict table, evidence, and known accepted risks: `docs/SECURITY.md`.

**Already in place, verified correct** (no change needed): upload size limits
(3 layers), XXE hardening (verified via live mutation test), decimal/invariant-culture
parsing, role-based authorization on every endpoint, login rate limiting, generic
error-handling *behavior* (status code + no leaked detail).

**Actually fixed this session**:

- Kestrel's global `MaxRequestBodySize` was never set (only the per-endpoint attribute).
- `docker-compose.yml` committed a real, working JWT fallback signing key — removed;
  `JWT_KEY` is now required, no default.
- JWT validation didn't pin `ValidAlgorithms` — added.
- Upload had zero rate limiting — added (20/min per user), which also surfaced and fixed
  a middleware-ordering bug (`UseRateLimiter` ran before `UseAuthentication`, so per-user
  partitioning would have silently degraded to per-IP).
- No CORS policy existed at all — added, locked to zero origins by default.
- No security headers existed at all — added `X-Content-Type-Options`, `X-Frame-Options`,
  a scoped CSP, and `UseHsts()` + `UseForwardedHeaders()` (needed because the Azure
  deployment target terminates TLS at the platform edge).
- The CSP required moving the Vite build's one inline script to an external file
  (`frontend/public/theme-init.js`) so `script-src 'self'` wouldn't silently break it.
- `GlobalExceptionHandler` was silently sending `application/json` instead of
  `application/problem+json` for unhandled-exception responses (found while writing the
  Stage C test for this exact behavior).
- `xunit`/`xunit.runner.visualstudio` bumped to clear a transitive vulnerable-package
  advisory (test-only, never shipped, but a real fixable finding from
  `dotnet list package --vulnerable`).

**N/A, documented**: CSV export doesn't exist in v1 — comment left at the natural future
location instead.

## Shared component inventory (`frontend/src/components/ui/`)

`alert`, `avatar`, `badge`, `button`, `card`, `dropdown-menu`, `input`, `label`, `tabs`,
`theme-toggle`, `toast`, `tooltip` — plus feature components in `components/upload/`
(DropZone, FileCard, FormatHintTabs, ProcessingScreen, UploadErrorScreen) and
`components/report/` (ReportHeader, SummaryCards, ValidationPanel,
CategoryBreakdownTable, ReportSkeleton). No new primitives were needed this session —
the report view composed entirely from Card/Badge/Button plus new feature-specific
components, per the "extend, never fork" rule.

## Technical improvements

- **Axios migration** (pure frontend refactor): every `fetch()` call in `frontend/src`
  replaced with a single central axios client (`api/client.ts`) — request interceptor
  attaches auth from the same token source `AuthContext` already used, response
  interceptor normalizes every rejection into one `ApiError` shape (status, title,
  detail, problemDetails — network/timeout failures included, as status `0`) and
  preserves the exact existing 401-redirect/login-exemption behavior. Zero observable
  behavior change, plus one explicit addition: a minimal hand-rolled session cache
  (`api/cache.ts`, no TanStack Query — noted as a possible future improvement) for
  `GET /api/countries/HU/vat-categories` and `GET /api/auth/me`, invalidated on logout
  and on any 401. `git grep "fetch(" frontend/src` returns nothing; axios is imported
  only in `client.ts`.

### Migration checklist (ticked)

| # | Endpoint | File(s) | Ticked |
|---|---|---|---|
| 1 | `POST /api/auth/login` | `client.ts` (`loginRequest`) | ✅ `client.ts` |
| 2 | `GET /api/auth/me` | `client.ts` (`getMe`) | ✅ `client.ts` — now cached; still unused by any page (see below) |
| 3 | `GET /api/health` | `client.ts` (`getHealth`) | ✅ `client.ts` — still unused by any page (see below) |
| 4 | `GET /api/declarations` | `client.ts` (`getDeclarations`) → `DashboardPage` | ✅ `client.ts` |
| 5 | `GET /api/declarations/{id}` | `client.ts` (`getDeclaration`) → `DeclarationReportPage` | ✅ `client.ts` |
| 6 | `POST /api/declarations` | `client.ts` (`uploadDeclaration`) → `UploadPage.process` | ✅ `client.ts` + `UploadPage.tsx` (migration-trap fix: `instanceof ApiError` → `status !== 0`) |
| 7 | `GET /api/countries/HU/vat-categories` | `client.ts` (`getVatCategories`) → `DeclarationReportPage` | ✅ `client.ts` + `cache.ts` (now the real cache, replacing the old ad-hoc per-country `Map`) |
| 8 | `GET /api/samples/{name}` | `client.ts` (`fetchSampleBlob`/`fetchSampleAsFile`/`downloadSample`) → `DropZone`, `FormatHintTabs`, `UploadErrorScreen` | ✅ `client.ts` (`responseType: 'blob'`) |
| 9 | `GET /api/declarations/{id}/pdf` | `client.ts` (`downloadDeclarationPdf`) → `DeclarationReportPage`, `DeclarationsTable` | ✅ `client.ts` (`responseType: 'blob'`) |

### Found during refactor (not fixed — flagging per session instructions)

- `getMe()` and `getHealth()` are fully implemented in the API client but **never called
  from any page or component** — dead code that predates this session. Migrated to axios
  (and `getMe` now goes through the cache) for consistency, but left unused as found;
  not something a pure refactor session should start wiring into new call sites.
- `FormatHintTabs.tsx` and `UploadErrorScreen.tsx`'s `handleDownload` have a `try/finally`
  with no `catch` — if `downloadSample` throws, it's an unhandled promise rejection
  (console warning, `downloading` state still resets via `finally`). Pre-existing,
  unchanged by the migration; left as-is per "don't fix silently."

- **`Program.cs` decomposition** (pure backend refactor): `Program.cs` shrunk from 217 to
  26 lines — now a table of contents (QuestPDF license → six `Add*` calls → `Build()` →
  one startup task → one pipeline call → `Run()`). All service registrations and the
  entire middleware pipeline moved into `src/VatDesk.Api/Extensions/`, one file per
  concern, with every existing order-dependency comment (forwarded headers before
  HSTS/HTTPS-redirection, security headers before static files/the SPA fallback, auth
  before the rate limiter) preserved verbatim rather than summarized. Proven
  behavior-identical by the still-green integration suite (93/93, same count as before)
  plus a live docker-compose pass: security headers on API responses, a static asset, and
  a genuine 404; anonymous 401 and viewer 403 on protected endpoints; login rate limiting
  tripping at the same threshold; the SPA fallback serving a deep link; and the fail-fast
  JWT-key startup guard producing the identical error message (now attributed to
  `AuthenticationServiceCollectionExtensions` in the stack trace — the one expected,
  harmless difference). Extension inventory:

  | Extension | Target | Concern |
  |---|---|---|
  | `AddPersistenceServices` | `IServiceCollection, IConfiguration` | DbContext + repositories |
  | `AddDomainServices` | `IServiceCollection` | Parsers, PDF renderer, the `AddCountry` extensibility seam |
  | `AddAuthenticationServices` | `IServiceCollection, IConfiguration` | JWT bearer + the fail-fast key guard |
  | `AddAuthorizationPolicies` | `IServiceCollection` | Bare `AddAuthorization()` — no named policies exist yet |
  | `AddApiSecurity` | `IServiceCollection, IConfiguration` | CORS + rate limiter policies |
  | `AddApiInfrastructure` | `WebApplicationBuilder` | Controllers, ProblemDetails, exception handler, Kestrel + multipart size limits (needs the full builder since Kestrel config lives on `.WebHost`) |
  | `ApplyMigrationsAsync` | `WebApplication` | The one startup task (EF Core migrations) |
  | `UseVatDeskPipeline` | `WebApplication` | The entire ordered middleware pipeline, kept as one method rather than split into stages since the ordering contract has non-local dependencies |

  Found during refactor, not fixed: `AddAuthorizationPolicies` has no named
  `AuthorizationPolicy` objects to configure — every role check is an
  `[Authorize(Roles=...)]` attribute on a controller. Kept the method (with that noted in
  its doc comment) so a future named policy has an obvious home, rather than folding it
  into another extension. No ordering bugs found in the existing pipeline — the two shapes
  the task specifically warned about (headers registered after the SPA fallback,
  rate-limiter before auth) were both already correct going in.
