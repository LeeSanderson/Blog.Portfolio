# 07 — Frontend CI/CD reusable workflow template

**What to build:** The reusable frontend deploy workflow and the per-app template that calls it, so a new app's frontend pipeline is a copy of a small template rather than a fresh design each time (ADR-0002: direct push to `leesanderson.github.io`, no PR gate).

**Blocked by:** 01

**Status:** done

- [x] A reusable workflow (`.github/workflows/_reusable-frontend-deploy.yml`) exists, encapsulating the common build-and-push-to-`leesanderson.github.io` steps
- [x] A documented per-app workflow template (`.github/workflows/templates/app-frontend.yml`) shows how a new app's `.github/workflows/{app-name}.yml` calls the reusable workflow, path-filtered to `apps/{app-name}/frontend/**`
- [x] The workflow YAML passes structural validation — `actionlint` clean on both files
- [x] The template and its usage are documented in `.github/workflows/templates/README.md` (linked from `apps/README.md`)
- [x] Live execution isn't verified by this ticket, since no frontend app exists in this repo yet — that will happen naturally when the first frontend-having app is built

## Comments

- The reusable workflow takes generic `install-command`/`build-command`/`build-output-dir`/`setup-node` inputs rather than hardcoding Node.js, since the spec allows any frontend framework (Next.js, Blazor WASM, Angular) per app — the template documents overriding these per app.
