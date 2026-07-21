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
- [ ] Phase 3 — Backend: Core models → parsers → validation (V1–V8) → HU strategy → PDF → API endpoints
- [ ] Phase 4 — Frontend: login, dashboard/history, upload, report view, PDF download, role-aware UI
- [ ] Phase 5 — Auth & authorization (baseline JWT + roles; Entra ID if time allows)
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
GET /api/declarations/{id} · GET /api/declarations/{id}/pdf
