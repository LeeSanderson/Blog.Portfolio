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
- xUnit for testing
- FluentAssertions for test assertions
- Moq for mocking
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

### Running the Azure Functions Locally

```powershell
cd host/src/Blog.Portfolio.Host
func start
```

## Code Quality

This solution uses:

- StyleCop Analyzers for code style enforcement
- .NET Analyzers for code quality analysis
- EditorConfig for consistent code style
- Directory.Build.props for common project settings

## Testing

No test projects exist yet — each app's backend carries its own under `apps/{app-name}/backend/tests/`, and `host/tests/` holds cross-app tests (e.g. the route-prefix architecture test). xUnit, FluentAssertions, and Moq are the established stack; coverlet reports coverage.

## API Endpoints

None yet — the project has been stripped back to a bare Azure Functions boilerplate.
