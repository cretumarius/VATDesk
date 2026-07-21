# AI conversation log

Unedited exports of every Claude session that produced this repository, per the
challenge's submission requirement. **The log files themselves are not in this commit —
they're added by hand after export** (this file and the folder are the scaffold; see
`docs/PLAN.md`'s deliverables checklist for the pending-manual-step note).

## Structure

One file per session, named to sort in the order the work happened. Exact filenames/
formats depend on how each session is exported (Claude.ai conversation export vs.
Claude Code session export) — the list below is the mapping this folder is built
around; rename to match what actually gets exported rather than forcing files to fit
these names.

| # | Session | Scope | Commits | Repo state |
|---|---|---|---|---|
| 00 | Planning (Claude.ai, not Claude Code) | Requirements analysis, NAV 3.0 XML / Hungarian VAT domain research, architecture decisions, the data contract | none — predates the repo | — |
| 01 | Skill + scaffold | `.claude/skills/hungarian-vat/` created, `docs/design/` added, backend/frontend walking skeletons, Docker packaging | `0d0cd69`…`5f94fb2` | Phase 0–2 |
| 02 | Phase 3 — backend domain | Core models → parsers → validation V1–V8 → HU strategy → PDF → API endpoints | `ad5b713`…`b791e3f` | Phase 3 |
| 03 | Phase 4.1 — auth, login, shell | JWT auth, role policies, login page, app shell | `3f6e936`…`bf746f7` | Phase 4.1 |
| 04 | Phase 4.2 — upload flow | Drag/drop, sample files, processing states | `aae0df3`…`69643f0` | Phase 4.2 |
| 05 | Phase 4.3 — report view | Summary cards, validation panel, category breakdown | `91f9b48`…`f5deb59` | Phase 4.3 |
| 06 | Dashboard bug fix | Declarations list wasn't wired up; fixed, which delivered Phase 4.4 as a side effect | `b68e686` | Phase 4.4 |
| 07 | Phase 6 — security audit + hardening | 11-point checklist: audit → fix → prove, one commit per finding | `7bcb2ce`…`6a8b877` | Phase 6 |
| 08 | Axios refactor | Pure frontend refactor: every `fetch()` → a central axios client + interceptors, plus a small explicit cache | `dbbfebd`…`074c322` | Technical improvement |
| 09 | `Program.cs` decomposition | Pure backend refactor: 217 lines → 26, service registrations and pipeline extracted into `Extensions/` | `e8c9fac`…`28fd525` | Technical improvement |
| 10 | Railway deployment | Railway MCP setup, deployment fix (broken service stub, JWT key config), favicon | `1358f22` (favicon; the Railway infrastructure work itself has no repo commits — it's MCP/CLI actions against Railway, not code changes) | Phase 7 (deployment) |
| 11 | Phase 7 — documentation & submission packaging (this session) | Azure→Railway doc reconciliation, `docs/DEPLOYMENT.md`, PLAN.md audit, README rewrite, this log scaffold | `8efd61b`…(final commit of this session) | Phase 7 |

Commit hashes are real and independently verifiable (`git log --oneline`); the
session-to-commit-range mapping is this project's own reconstruction from session
summaries carried between sessions, not from re-reading raw transcripts — accurate to
the best of what's recorded, but if you export sessions and find a boundary drawn
differently than above, trust your export over this table and adjust it.

## Verifying a session against the repo

Each row's commit range is checkable independently of the log file itself:

```bash
git log --oneline <first-commit>^..<last-commit>
```

The commit messages in this project consistently describe what was done and why —
per `CLAUDE.md`'s workflow conventions, that's deliberate: the commit history is meant
to stand on its own as a factual record even without the full conversation log.
