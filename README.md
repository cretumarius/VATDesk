# VATDesk

Hungarian VAT (ÁFA) declaration generator. Ingests invoice/transaction files (CSV or
NAV 3.0-style XML), validates them, aggregates amounts by Hungarian VAT category, and
produces a declaration summary — on-screen and as a PDF.

<!-- TODO: one-paragraph pitch + screenshot once the UI is implemented (Phase 4) -->

## Overview

<!-- TODO: expand once parsing/validation/strategy/PDF land (Phase 3 continued) -->

- Backend: ASP.NET Core 8 Web API (`src/VatDesk.Api`, `src/VatDesk.Infrastructure`,
  `src/VatDesk.Core`)
- Frontend: React 18 + TypeScript + Vite (`frontend/`), served as static files by the
  API in production — single deployable unit
- Database: PostgreSQL via EF Core + Npgsql; migrations apply automatically on startup
- Country model: pluggable per-country VAT registry + declaration strategy; Hungary
  (`HU`) is the only implemented country in v1

## Quick start

```bash
cp .env.example .env
# then edit .env and set JWT_KEY to a real generated value:
#   openssl rand -base64 48
docker compose up --build
```

This starts PostgreSQL and the app (API + built frontend served together) at
`http://localhost:8080`. `GET /api/health` reports app version and DB connectivity.

`.env` is required — there is no built-in fallback signing key (see
[Security](#security)). The app fails fast at startup with a clear error if `JWT_KEY` is
missing or shorter than 32 bytes.

For frontend hot-reload during development, run the API and the Vite dev server
separately:

```bash
dotnet run --project src/VatDesk.Api
cd frontend && npm install && npm run dev
```

The Vite dev server proxies `/api/*` to the API (see `frontend/vite.config.ts`).

### Environment variables

| Variable | Required | Purpose |
|---|---|---|
| `JWT_KEY` | yes | HS256 signing key, ≥ 32 bytes. No default — generate with `openssl rand -base64 48`. |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | no (demo defaults) | Local-only Postgres credentials for docker-compose; not internet-facing. |

## Demo credentials

<!-- TODO: seed admin@demo.hu / viewer@demo.hu once auth is implemented (Phase 5) -->

## Architecture

Layering: `Api → Infrastructure → Core`, enforced by project references. `Core` has
zero external dependencies.

```
src/
├── VatDesk.Core/            # models, abstractions, validation — no external deps
├── VatDesk.Infrastructure/  # EF Core persistence, parsers, country strategies, PDF
└── VatDesk.Api/             # controllers, auth, DTOs
frontend/                    # React app (Vite)
tests/VatDesk.Tests/         # xUnit
```

See `.claude/skills/hungarian-vat/references/architecture.md` for the full layout,
persistence schema, and API surface.

<!-- TODO: architecture diagram once the feature set is complete -->

## Security

Hardened against the 11-point checklist in
`.claude/skills/hungarian-vat/references/architecture.md` — upload limits (Kestrel +
endpoint + multipart, all three layers), XXE-hardened XML parsing, invariant-culture
decimal parsing, JWT auth (HS256-pinned, fail-fast key check, no committed secrets),
server-enforced role authorization, rate limiting on login and upload, CORS, HSTS/CSP/
security headers on every response (including behind Azure's TLS-terminating edge), and
generic error responses with no leaked stack traces.

Full verdict per item, evidence, the two extra audits (declaration-id enumeration
reasoning, dependency scan results), and known accepted risks for demo scope: see
[**docs/SECURITY.md**](docs/SECURITY.md).

## Extending to a new country

Each country is one `IVatCategoryRegistry` + one `IVatDeclarationStrategy`,
registered in DI via `AddCountry<TRegistry, TStrategy>("XX")`. No changes should be
needed in `Core`, controllers, parsers, or the React components — if a change is
needed there, the abstraction leaked.

<!-- TODO: fill in with the Romania walkthrough once implemented -->

## AI-assisted workflow

This project was built with Claude Code using a custom project skill
(`.claude/skills/hungarian-vat/`) encoding the domain rules, data contracts, and
architecture conventions, plus a locked project plan (`docs/PLAN.md`).

<!-- TODO: link the full AI conversation log on submission -->
