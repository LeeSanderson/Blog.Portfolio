# apps/

One folder per portfolio app: `apps/{app-name}/`. An app may have a `backend/` (with `src/` and `tests/`), a `frontend/` (with `src/` and, only where the framework needs a separate test project, `tests/`), or both, plus its own `README.md`.

Adding a frontend to an app? Copy the CI/CD template at `.github/workflows/templates/app-frontend.yml` — see
`.github/workflows/templates/README.md`.
