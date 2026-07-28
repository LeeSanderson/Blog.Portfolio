# Restructure into a Multi-App Portfolio Monorepo (Blog.Portfolio)

Status: done

## Problem Statement

Lee wants to keep building and shipping small portfolio/example projects on sixsideddice.com — games, standalone tools, backend-only utilities, and full-stack demos — but the current `Blog.Api` repo is shaped for exactly one thing: a single blog backend. There's no folder convention for adding a second, unrelated project; no shared way to run a project's frontend and backend together locally; no shared library location for code reused across projects; and no repeatable way to provision or deploy the infrastructure. Today, every new portfolio idea means either bolting unrelated code into a single-purpose repo, or spinning up an entirely separate repo (as happened with BuzzerBee) and losing any shared tooling, conventions, or infrastructure with everything else.

## Solution

Restructure and rename the repo (`Blog.Api` → `Blog.Portfolio`) into a monorepo that can hold any number of **Apps** — each with its own `backend/`, `frontend/`, or both — while every app's backend code deploys as one shared Azure Function App (the **Host**). Local development for the whole suite runs through a single .NET Aspire AppHost. Adding a new app becomes a matter of following an established folder convention and copying a working example, rather than inventing structure each time.

## User Stories

1. As Lee, I want a top-level `apps/` folder convention, so that I can add a new portfolio app without inventing a new folder structure each time.
2. As Lee, I want each app to optionally have a `backend/` folder, so that backend-only apps (e.g. a newsletter signup endpoint) don't carry an unused frontend folder.
3. As Lee, I want each app to optionally have a `frontend/` folder, so that pure-frontend apps (e.g. a game) don't carry an unused backend folder.
4. As Lee, I want backend source code to live in `apps/{app}/backend/src/` and tests in `apps/{app}/backend/tests/`, so that backend code follows the same separate-test-project convention already used elsewhere in the repo.
5. As Lee, I want frontend source code to live in `apps/{app}/frontend/src/`, with tests colocated per that framework's own convention (e.g. Jest/Vitest for Next.js, Angular CLI `*.spec.ts`) or in `apps/{app}/frontend/tests/` where the framework needs a separate test project (e.g. Blazor + bUnit), so that frontend testing doesn't fight the idioms of whichever framework an app uses.
6. As Lee, I want a dedicated `host/` project that is not itself an app, so that composition-root wiring (OpenAPI, CORS, Application Insights, JSON config) has an obvious home distinct from any single app's business logic.
7. As Lee, I want the `host/` project to automatically discover and expose `[Function]`-attributed endpoints defined in any app's `backend/src` project, so that adding a new app's backend requires only a project reference, not a manual registration step.
8. As Lee, I want every app's HTTP endpoints reachable under an `/api/{app-name}/...` route prefix, so that two apps can never accidentally collide on the same route as the portfolio grows.
9. As Lee, I want an automated check that scans every app's compiled backend assembly and fails the build if an endpoint's route doesn't start with `/api/{app-name}/`, so that the route-prefix convention is enforced without relying on developer memory or a rigid compiler-enforced base class.
10. As Lee, I want a shared `Endpoint<TRequest, TResponse>`-style REPR base class in `shared/backend/`, so that every app's backend endpoints follow the same request/endpoint/response shape without hand-rolling boilerplate per endpoint.
11. As Lee, I want a `shared/backend/` location for cross-app backend libraries (e.g. a future OpenRouter AI chat client), so that logic genuinely reused by multiple apps has one home instead of being duplicated or awkwardly owned by one app.
12. As Lee, I want `shared/frontend/{framework}/` locations created only once a second app in that framework needs shared UI code, so that I don't speculatively build shared frontend libraries for frameworks with a single consumer.
13. As Lee, I want to run the entire suite of apps locally via one .NET Aspire AppHost, so that I can see the shared backend host and every app's frontend running together without juggling separate terminals and manual configuration.
14. As Lee, I want each app registered explicitly in the AppHost's code, so that I can comment out or conditionally skip an app I don't want running locally today, without a separate app-selection mechanism.
15. As Lee, I want the AppHost to start `host/` via the Azure Functions Aspire integration (`AddAzureFunctionsProject`, not a generic project resource), so that the Functions runtime and Azurite storage emulator are wired up automatically for local development.
16. As Lee, I want each app's frontend to have its own dedicated GitHub Actions workflow, path-filtered to that app's `apps/{app}/frontend/`, so that a change to one app's frontend doesn't trigger builds for unrelated apps.
17. As Lee, I want each app's frontend CI workflow to call a shared reusable workflow for the common build/push steps, so that adding a new app's pipeline means copying a small template, not re-authoring the whole thing.
18. As Lee, I want a frontend app's CI to push its built static output directly to a target folder in the `leesanderson.github.io` repo on merge to `main`, so that shipping a new build to the live site requires no manual copy step.
19. As Lee, I want exactly one GitHub Actions workflow for the whole backend (not one per app), triggered by a change anywhere under any app's `backend/` or under `shared/backend/`, so that the workflow structure matches the fact that all backend code deploys as a single Azure Function App.
20. As Lee, I want the backend workflow to run the full test suite across every app's backend code (not just the changed app's tests) before deploying, so that a change to one app can't silently break another app sharing the same deployed process.
21. As Lee, I want the backend to auto-deploy to production on a green CI run, with no manual approval gate, so that shipping a backend change is as fast and frictionless as shipping a frontend change.
22. As Lee, I want Azure infrastructure (the Function App, its storage account, Application Insights) provisioned as Bicep generated from the Aspire AppHost model and deployed via `azd`, so that I can expand or recreate the infrastructure easily as new apps and their supporting resources are added.
23. As Lee, I want app-specific configuration keys to be free-form rather than namespaced or enforced per app, so that a setting genuinely shared across multiple apps (e.g. one OpenRouter API key used by several apps) doesn't have to be artificially duplicated per app.
24. As Lee, I want the repo, solution file, and root namespace renamed from `Blog.Api` to `Blog.Portfolio`, so that the repo's name reflects what it actually contains rather than implying a single blog backend.
25. As Lee, I want existing separate-repo pure-frontend projects (e.g. BuzzerBee) to remain untouched by this restructure, so that I'm not forced into unrelated migration work with no functional benefit right now.
26. As Lee, I want a minimal example app (`apps/example/`) included as part of this restructure, so that there's a concrete, working reference for the conventions above, and a vehicle the automated tests can exercise end-to-end.
27. As Lee, I want an integration test that starts the real AppHost and calls the example app's endpoint over HTTP, so that cross-assembly function discovery, route prefixing, and the REPR base class are all proven to work together, not just individually.
28. As Lee, I want the existing `src/Blog.Api.Functions`, `tests/Blog.Api.UnitTests`, and `tests/Blog.Api.IntegrationTests` projects retired in favour of the new structure, so that the repo doesn't carry two parallel, conflicting conventions for where backend code lives.
29. As a future contributor (human or agent) adding a new app, I want a documented folder convention and a working example app to copy, so that I can scaffold a new app correctly without reverse-engineering the pattern from scratch.
30. As Lee, I want the CI/CD, Aspire, and IaC pieces of this restructure proven to work even before any "real" portfolio app exists, so that the platform is solid before the first actual showcase project is built on top of it.

## Implementation Decisions

- Rename the repo, solution file, and root namespace from `Blog.Api` to `Blog.Portfolio` (ADR-0003).
- Retire the root `src/` and `tests/` folders. Replace with four top-level folders: `apps/`, `shared/`, `host/`, `aspire/`.
- `host/` becomes the sole composition-root Azure Functions isolated-worker project (successor to `Blog.Api.Functions`), carrying forward its existing OpenAPI, CORS, Application Insights, and JSON serialization configuration. It references every app's `backend/src` project; because a referenced class library doesn't generate its own "default" function metadata provider, its `[Function]`-attributed endpoints surface through `host`'s generated provider automatically (ADR-0001).
- Every app under `apps/{app-name}/` may have a `backend/` (with `src/` and `tests/`), a `frontend/` (with `src/` and, only where the framework needs a separate test project, `tests/`), or both, plus a `README.md`.
- `shared/backend/` holds cross-app .NET libraries, starting with the REPR base abstraction. `shared/frontend/{framework}/` folders are created lazily — none exist yet.
- The REPR base abstraction is an `Endpoint<TRequest, TResponse>`-shaped base class in `shared/backend/`, which each app's `[Function]`-attributed method adapts to (ADR-0005). Exact member signatures are left to implementation.
- Every HTTP-triggered endpoint's route must start with `/api/{app-name}/`. This is not enforced by the base class itself — enforcement is a separate architecture test (see Testing Decisions).
- Configuration keys are intentionally not namespaced or enforced per app; keys shared across apps are permitted by design.
- A new `aspire/` folder holds `Blog.Portfolio.AppHost` and `Blog.Portfolio.ServiceDefaults` projects. `host/` is registered in the AppHost via `AddAzureFunctionsProject<THost>()` (not `AddProject<T>()`, which doesn't start a Functions project correctly). Every app is registered explicitly, one call per resource, directly in AppHost code — no folder auto-discovery.
- CI/CD: one GitHub Actions workflow per app for its frontend (`.github/workflows/{app-name}.yml`, path-filtered to `apps/{app-name}/frontend/**`), calling a shared reusable workflow for the common build/push steps. One `.github/workflows/backend.yml`, triggered by changes under any app's `backend/**` or under `shared/backend/**`, builds, tests, and deploys the single combined `host` Function App.
- Frontend deploy: the per-app workflow pushes built static output directly to a target folder in the separate `leesanderson.github.io` repo on merge to `main` — no pull request review step (ADR-0002).
- Backend deploy: auto-deploy to the shared Azure Function App on green CI, gated by the full cross-app backend test suite — no manual approval step (ADR-0001).
- Infrastructure as code: Azure resources (Function App, storage account, Application Insights) are defined via Bicep generated/maintained from the Aspire AppHost model under a new `infra/` folder, plus an `azure.yaml` at the repo root, deployed via `azd` (ADR-0004).
- A minimal example app, `apps/example/`, is scaffolded as part of this work: a single backend endpoint (e.g. `GET /api/example/ping`) built on the shared REPR base, following the full folder convention. It exists purely as a walking-skeleton reference and test vehicle, not a real portfolio showcase.
- Existing separate-repo pure-frontend projects (e.g. BuzzerBee) are explicitly untouched by this work — no retroactive migration.

## Testing Decisions

- Tests should exercise external behavior through the seams below, not the internal implementation details of the REPR base class, the discovery mechanism, or the AppHost wiring individually.
- **Primary seam**: an integration test using `Aspire.Hosting.Testing.DistributedApplicationTestingBuilder` builds and starts the real AppHost model, resolves an `HttpClient` for the `host` resource, and issues a real HTTP request to the example app's endpoint (`GET /api/example/ping`). This single seam proves cross-assembly function discovery, the route-prefix convention, the REPR base class's request/response handling, and the AppHost wiring together, in one test.
- **Secondary seam** (deliberately kept separate, per ADR-0001): a reflection-based architecture test scans the compiled `host` assembly and every referenced app `backend` assembly, asserting every `[Function]`-attributed method's route starts with `/api/{app-name}/`. It must stay independent of the running-host seam because it needs to hold for every current and future app, not just whichever app happens to be wired into a given integration test run.
- Prior art in this repo: the existing `tests/Blog.Api.UnitTests` and `tests/Blog.Api.IntegrationTests` projects (xUnit, FluentAssertions, Moq) establish the testing stack to carry forward; the project shells are retired, but their tooling choices continue in the new `host/tests/` and `apps/example/backend/tests/` projects.
- No frontend tests are needed for this spec, since `apps/example/` is backend-only.
- CI workflow correctness is validated by the workflows actually running in GitHub Actions once merged, not by a separate unit-test seam.

## Out of Scope

- Building any real, publicly-showcased portfolio app — a separate, future piece of work once this skeleton exists.
- Migrating existing separate-repo projects (e.g. BuzzerBee) into this monorepo.
- `shared/frontend/{framework}/` folders — none created until a second app in a given frontend framework needs shared UI code.
- The OpenRouter AI chat client mentioned as a future `shared/backend/` component.
- Any specific frontend framework tooling setup (Next.js/Blazor/Angular templates) beyond what's needed for the backend-only `apps/example/` vehicle.
- Multi-environment (staging/prod) infrastructure topology — the IaC introduced here targets the single existing environment.

## Further Notes

- Full architectural context and rationale live in `CONTEXT.md` (glossary: **App**, **Host**) and `docs/adr/0001`–`0005`, produced via a `/grill-with-docs` session preceding this spec. Implementers should read those before starting.
- `apps/example/` is deliberately trivial and should be easy to delete or repurpose once a real first portfolio app exists, but is expected to remain for a while as the reference template for scaffolding new apps.
- The `aspire/` folder location for `AppHost`/`ServiceDefaults` was proposed while writing this spec (not in the original grilling session) and confirmed by Lee at that point.
