## Coding standards

Avoid code comments by default — prefer clean, well-named classes, methods,
and variables to carry intent. Only comment when the code genuinely can't say
it: an obscure algorithm, a non-obvious constraint, or the reasoning behind a
decision that would otherwise be lost. Never comment *what* the code does.
See `CODING_STANDARDS.md` for the full set of conventions.

## Agent skills

### Issue tracker

Issues and specs are tracked as local markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical role names used as-is (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.

### Running locally

Use `./run-local.ps1` from the repo root to run the whole stack (Host + Azurite) for manual or agent-driven
testing — the Host is reachable at `http://localhost:7240`. Don't use bare `func start` for anything that
touches storage (e.g. email-subscription); it starts no storage emulator.
