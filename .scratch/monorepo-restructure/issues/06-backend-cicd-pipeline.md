# 06 — Backend CI/CD: full-suite gate + auto-deploy

**What to build:** The single shared-backend pipeline: on any change under an app's `backend/` or `shared/backend/`, build the whole combined host, run every backend test across every app (both seams from tickets 03/04 plus any app-level tests), and auto-deploy to production on green — no manual approval gate (ADR-0001).

**Blocked by:** 03, 04, 05

**Status:** ready-for-agent

- [ ] `.github/workflows/backend.yml` exists, triggered by changes under any app's `backend/**` or `shared/backend/**`
- [ ] The workflow builds the solution and runs the full backend test suite (architecture test from 03, integration test from 04, and any app-level backend tests) before deploying
- [ ] On green CI on `main`, the workflow deploys `host/` to the Azure Function App provisioned in 05, with no manual approval step
- [ ] A failing test in any app's backend blocks the deploy
- [ ] After a successful deploy, the live `/api/example/ping` endpoint responds as expected

## Comments

- Still blocked: ticket 05's Bicep is written but not yet provisioned (needs Lee to run `azd provision` — see `infra/README.md`). Pick this up once that Function App exists in Azure, since this workflow's deploy target and its "confirm the live endpoint" acceptance criterion both depend on it being real.
