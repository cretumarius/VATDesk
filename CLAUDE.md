# VATDesk — Hungarian VAT Declaration Generator

Coding challenge project (TaxDesk). ASP.NET Core 8 API + React/TypeScript (Vite) +
PostgreSQL, single Docker deployable, deployed on Railway (originally targeted Azure;
see `docs/PLAN.md`'s locked decisions and `docs/DEPLOYMENT.md` for the pivot and what
an Azure deployment would look like instead).

## Read these before non-trivial work

- `docs/PLAN.md` — the agreed plan: locked decisions, phase status, scope guardrails,
  deliverables checklist. Update phase checkboxes as work completes.
- `.claude/skills/hungarian-vat/` — project skill with domain rules. Its SKILL.md loads
  automatically; its `references/` hold the data contract, VAT category registry, and
  architecture/security conventions. Follow them exactly — consistency of field names,
  codes, and tolerances is a hard requirement.
- `docs/design` the design of the app built with Claude design

## Hard rules (duplicated here for safety)

- Money is `decimal`, invariant culture, HUF only in v1.
- VAT codes: 27, 18, 5, 0, AAM, TAM, EUFAD, FAD — from the registry, never hard-coded
  elsewhere.
- XML parsing: DtdProcessing.Prohibit + XmlResolver = null, 5 MB cap. Never relax.
- Layering: Api → Infrastructure → Core; Core has zero external dependencies.
- Roles enforced server-side (Admin uploads/generates, Viewer reads).
- Use skill `assets/` sample files as canonical fixtures; golden values are in
  `references/data-contract.md`.

## Workflow conventions

- Conventional commits (feat/fix/chore/docs/test).
- Every completed phase: update `docs/PLAN.md` checkbox in the same commit.
- All Claude sessions on this repo are part of the submitted AI log — keep prompts and
  iterations honest; no history rewriting.
