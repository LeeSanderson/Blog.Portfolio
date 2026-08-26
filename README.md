# Blog.Portfolio

A monorepo hosting a growing portfolio of small example and demo projects for sixsideddice.com, sharing one deployable backend and a common set of local-dev and delivery conventions. See `CONTEXT.md` for the domain glossary and `docs/adr/` for the architectural decisions behind this structure.

## Project Structure

- **apps/**: one folder per portfolio app (`apps/{app-name}/`), each optionally with its own `backend/` and/or `frontend/`
- **shared/**: cross-app libraries (`shared/backend/`, `shared/frontend/{framework}/`)
- **host/**: the composition-root Azure Functions project (`Blog.Portfolio.Host`) that discovers and exposes every app's backend endpoints
- **aspire/**: the .NET Aspire AppHost used to run the whole suite locally
- **infra/**: Bicep for the shared Azure Function App, provisioned and deployed via `azd` (see `infra/README.md`)

## Technologies

- .NET 10
- Azure Functions
- xUnit v3 for testing
- AwesomeAssertions for test assertions
- NSubstitute for mocking
- StyleCop for code analysis
- EditorConfig for code style enforcement

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure Functions Core Tools
- Visual Studio 2022 or Visual Studio Code

### Building the Solution

```powershell
dotnet build
```

### Running the Tests

```powershell
dotnet test
```

### Running Everything Locally

```powershell
./run-local.ps1
```

Starts the Aspire AppHost, which runs the Host together with an Azurite storage emulator — needed for
anything backed by Table or Queue storage (e.g. all of email-subscription). The Host is reachable at
`http://localhost:7240`.

Running `func start` directly from `host/src/Blog.Portfolio.Host` also works, but starts no storage
emulator, so only endpoints that don't touch storage will function correctly.

The first time you run this, the Aspire dashboard needs a trusted local HTTPS dev certificate. If it fails
to load with an `UntrustedRoot` SSL error, trust the certificate once with:

```powershell
dotnet dev-certs https --trust
```

## Code Quality

This solution uses:

- StyleCop Analyzers for code style enforcement
- .NET Analyzers for code quality analysis
- EditorConfig for consistent code style
- Directory.Build.props for common project settings

## Testing

Each app's backend carries its own tests under `apps/{app-name}/backend/tests/` (e.g. `Blog.Portfolio.Apps.Example.Backend.Tests`). Cross-app tests live under `host/tests/` (the route-prefix architecture test) and `aspire/Blog.Portfolio.AppHost.Tests/` (the end-to-end test that starts the real AppHost and calls an app's endpoint over HTTP). xUnit v3, AwesomeAssertions, and NSubstitute are the established stack; coverlet reports coverage.

## API Endpoints

- `GET /api/example/ping` — walking-skeleton endpoint from `apps/example/`, proving the REPR base class and route-prefix convention. See `apps/example/README.md`.
