# Frontend CI/CD template

Every app's frontend gets its own workflow, path-filtered to that app's `apps/{app-name}/frontend/`, calling
the shared `_reusable-frontend-deploy.yml` workflow for the common build-and-push steps (ADR-0002: direct
push to `leesanderson.github.io`, no PR gate).

## Adding a new app's frontend pipeline

1. Copy `app-frontend.yml` to `.github/workflows/{app-name}.yml`.
2. Replace every `<app-name>` placeholder with the real app name.
3. Uncomment and adjust any of the commented `with:` inputs the app's framework needs (build output
   directory, install/build commands, whether Node.js setup is needed at all).
4. Make sure the `PAGES_DEPLOY_TOKEN` repository secret exists (a PAT with push access to
   `leesanderson/leesanderson.github.io`) — it's shared across every app's frontend workflow, not per-app.

## Reusable workflow inputs

See `.github/workflows/_reusable-frontend-deploy.yml` for the full list of inputs and their defaults. The
workflow checks out the app repo, installs and builds the frontend, checks out the target repo, copies the
build output into `target-path`, and commits/pushes straight to `target-branch` — matching the existing
BuzzerBee deploy pattern.

Live execution isn't verified until the first frontend-having app actually exists in this repo — there is
nothing to build or deploy yet.
