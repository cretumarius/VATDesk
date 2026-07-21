# VATDesk — Security

Phase 6 security audit and hardening pass against the 11-point checklist in
`.claude/skills/hungarian-vat/references/architecture.md`. This document is the final
state after that session: audit → fix → prove, in that order (see `docs/PLAN.md` and the
Phase 6 commit history for the full trail — every fix below is its own commit referencing
the checklist item number).

## Checklist status

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | Upload limits | **Implemented** | Three independent layers: Kestrel global `Limits.MaxRequestBodySize` ([Program.cs](../src/VatDesk.Api/Program.cs)), per-endpoint `[RequestSizeLimit]` on `DeclarationsController.Upload`, and `FormOptions.MultipartBodyLengthLimit` — all set to the same 5 MB (`ParserFactory.MaxFileSizeBytes`). Extension allow-list and content-sniffing both genuinely reject with 400, not just log (tested). |
| 2 | XML / XXE | **Implemented** | `NavXmlInvoiceParser`: `DtdProcessing.Prohibit` + `XmlResolver = null` + `MaxCharactersInDocument = 5_000_000`, all in the one active parse path. Verified live: temporarily weakening `DtdProcessing.Prohibit` alone still left the XXE test passing (`XmlResolver = null` independently blocks it) — genuine defense-in-depth, not a single point of failure. |
| 3 | Parsing (decimal / invariant culture) | **Implemented** | Zero `double`/`float` in `VatDesk.Core`/`VatDesk.Infrastructure` production code. `FieldParsing.cs` and `CsvInvoiceParser`'s `CsvConfiguration` both use `CultureInfo.InvariantCulture`. Frontend never re-parses amounts, only formats server-supplied numbers. |
| 4 | AuthN (JWT) | **Implemented** | Key from env only (`Jwt__Key`), fail-fast at startup on missing/<32-byte key, 30-min expiry, HS256 pinned on both issuance (`SigningCredentials`) and validation (`ValidAlgorithms = [HmacSha256]`). `docker-compose.yml`'s committed fallback signing key was removed — `JWT_KEY` is now required, with no default. |
| 5 | AuthZ | **Implemented** | Full endpoint inventory checked against the skill's API table — every route has the correct role policy; no accidentally-anonymous endpoint. `GET /api/health` is intentionally anonymous (standard load-balancer probe pattern). |
| 6 | Rate limiting | **Implemented** | Login: 8/min, partitioned by client IP (pre-auth). Upload: 20/min, partitioned by authenticated user id — added this session, along with reordering `UseAuthentication`/`UseAuthorization` before `UseRateLimiter` (the original order made claims-based partitioning silently degrade to IP-based). |
| 7 | CORS | **Implemented** | Explicit named policy, locked to zero origins by default (never `*`); opts in exactly one origin via `Cors:AllowedOrigin` config if a split-origin deployment is ever needed. Moot in the actual prod topology (single-origin), but configured rather than omitted, per the checklist. |
| 8 | Headers | **Implemented** | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and a CSP scoped to what the app actually loads (verified against the built frontend, not guessed) on every response — API, static files, and the SPA fallback alike. `UseHsts()` + `UseForwardedHeaders()` for the Railway PaaS topology (TLS terminates at the platform edge — deployment target changed from Azure to Railway during Phase 7, but this reasoning holds for either). See [Headers section](#headers-in-detail) below. |
| 9 | Errors | **Implemented** | `GlobalExceptionHandler` always returns a generic ProblemDetails body with no exception detail (unconditional — not just prod-gated) and logs full detail server-side. A real bug was found and fixed here: `WriteAsJsonAsync`'s default overload was silently overwriting the `application/problem+json` content type back to plain `application/json`. |
| 10 | CSV export | **N/A** | No CSV/spreadsheet export exists in v1 (PDF only). A code comment documenting the required formula-injection defense (`=`/`+`/`-`/`@` cell prefix) is in place at the natural location a future export endpoint would live (`DeclarationsController.cs`). |
| 11 | Secrets | **Implemented** | `.env` git-ignored, `.env.example` tracked with generation instructions, docker-compose consumes everything via env-var substitution, `appsettings.json`'s connection string is a trivial local-only default overridden by env var in any real run. The one real gap found — a working JWT key committed as `docker-compose.yml`'s fallback default — is fixed (see #4). |

## Headers in detail

Live example (docker compose, simulating a TLS-terminating platform edge like Railway's
or Azure's with `X-Forwarded-Proto: https` and a real hostname):

```
HTTP/1.1 200 OK
Strict-Transport-Security: max-age=2592000
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'
```

CSP notes (scoped to what the app genuinely loads, checked against the built
`frontend/dist/index.html`, not assumed):

- `script-src 'self'` only — the Vite build's one inline script (a pre-paint theme-flash
  prevention snippet) was moved to `frontend/public/theme-init.js` specifically so this
  could stay strict, with no `'unsafe-inline'` for scripts.
- `style-src` needs `'unsafe-inline'` — Radix UI (dropdown menu, tooltip) sets inline
  `style` attributes at runtime for floating-element positioning. Much lower risk than
  `'unsafe-inline'` for scripts (can't execute JS directly) and an extremely common,
  accepted tradeoff for any app using Radix/Floating UI.
- `style-src`/`font-src` allow Google Fonts' two hosts — the only cross-origin resource
  the app actually loads (IBM Plex Sans/Mono).
- `connect-src 'self'` — every `fetch()` call in the frontend goes to a relative
  `/api/...` path.

`UseHsts()` only sets the header when `Request.IsHttps` is true, which is only ever true
directly if the request came in over real TLS — verified empirically, not assumed
(a minimal repro confirmed this, and separately confirmed that `HstsMiddleware` skips a
`Host` value of literally `"localhost"` regardless of scheme, which is why a plain
`curl http://localhost:8080/...` never shows the header locally — expected, not a defect).
Since the deployment target terminates TLS at the platform edge and forwards plain HTTP,
`UseForwardedHeaders()` (reading `X-Forwarded-Proto`/`X-Forwarded-For`) was added so
`IsHttps` — and the login rate limiter's per-IP partitioning — reflect the real client,
not the edge. `KnownNetworks`/`KnownProxies` are cleared since the specific edge IPs
aren't known in advance (see [Known accepted risks](#known-accepted-risks-for-demo-scope)).

## Two additional audits (not on the checklist)

### (a) Declaration/PDF id enumeration

`GET /api/declarations/{id}` and `/pdf` have no ownership check — any authenticated user
(Admin or Viewer) can fetch any declaration by GUID, since `DeclarationRepository`
doesn't filter by `UserId`. **This is intended, not a gap.** VATDesk has no
multi-tenancy: it's one shared workspace, and `GET /api/declarations` already returns the
*entire unfiltered list* to every authenticated Viewer. A Viewer guessing an id gains
nothing they don't already have through the list endpoint. GUID non-enumerability is
irrelevant here because the real (and correctly enforced) boundary is "must be
authenticated," not "must own this record." This becomes a real question only if
multi-tenancy is ever added — out of this session's and this app's current scope.

### (b) Dependency scan

- `dotnet list package --vulnerable --include-transitive`: **clean** across all four
  projects. (`VatDesk.Tests` initially flagged `System.Net.Http 4.3.0` and
  `System.Text.RegularExpressions 4.3.0`, both High — traced via `dotnet nuget why` to
  `xunit 2.5.3` → `NETStandard.Library 1.6.1`, a netstandard1.x compatibility metapackage
  never actually loaded at runtime on .NET 8. Low practical risk — test-only, never
  shipped in the Docker image — but fixed anyway: `xunit` → `2.9.3`,
  `xunit.runner.visualstudio` → `2.8.2`, both within the same major version.)
- `npm audit`: **0 vulnerabilities.**

## Known accepted risks for demo scope

- **JWT in `sessionStorage`** (from Phase 4.1): the token lives primarily in memory
  (`AuthContext` React state); `sessionStorage` is only a rehydration mechanism so a page
  refresh doesn't force a re-login. Anything reachable via `sessionStorage` is readable by
  an XSS payload, same as `localStorage`. An httpOnly cookie would be the safer pattern,
  but needs server-issued cookies + CSRF handling this demo's scope doesn't build.
  `sessionStorage` (vs. `localStorage`) at least scopes the exposure to the tab's
  lifetime. See `frontend/src/lib/auth-storage.ts`.
- **No multi-tenancy**: one shared workspace: every authenticated user (Admin or Viewer)
  sees every declaration. See the id-enumeration audit above — this is a design decision,
  not an oversight.
- **Demo credentials by design**: `admin@demo.hu` / `Admin123!` and `viewer@demo.hu` /
  `Viewer123!` are real, seeded, hashed-password accounts (not a client-only fake login) —
  intentionally documented and easy to guess, for reviewer convenience on a coding
  challenge. Never do this for a real deployment.
- **Local-only Postgres credentials**: `docker-compose.yml`'s `POSTGRES_USER`/
  `POSTGRES_PASSWORD`/`POSTGRES_DB` default to `vatdesk`/`vatdesk`/`vatdesk` if unset.
  Unlike the JWT key (removed from git entirely — see #4/#11), this default was kept:
  Postgres isn't internet-facing in this topology (only the `app` service is meant to be
  reachable), so the blast radius of a leaked default is far smaller. Still overridable
  via `.env`.
- **`ForwardedHeadersOptions.KnownNetworks`/`KnownProxies` cleared**: trusts
  `X-Forwarded-Proto`/`X-Forwarded-For` from any remote address, since the deployment
  platform's edge IPs (Railway's, or Azure's if redeployed there) aren't known in
  advance. Safe under the assumption that a trusted proxy (the platform edge) always
  sits in front in any real deployment; direct internet exposure of this container
  bypassing that edge is out of scope for a demo app.
- **CSP `style-src 'unsafe-inline'`**: required for Radix UI's runtime-injected
  positioning styles (dropdown menu, tooltip). See [Headers in detail](#headers-in-detail).

## Skill checklist gap (surfaced, not silently fixed)

The skill's own API table in `architecture.md` doesn't list `GET /api/samples/*` or
`GET /api/health` at all — both were added in later sessions (4.2, 3) after the checklist
was written. Both were audited under item 5 anyway (samples: `[Authorize]`, any
authenticated user; health: intentionally anonymous) and found correct, but the skill
document itself is out of date. Not fixed in this session — flagging per the "surface
skill gaps, don't silently exceed scope" instruction rather than editing the skill.
