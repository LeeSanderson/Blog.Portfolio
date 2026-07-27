# infra/

Bicep for the shared Azure Function App (`host/`), its storage account, and Application Insights, deployed via
the Azure Developer CLI (`azd`). See ADR-0004 for why this is hand-authored rather than generated from the
Aspire AppHost model.

- `main.bicep` — subscription-scoped entry point: creates the resource group, wires up the modules below.
- `appinsights.bicep` — Log Analytics workspace + Application Insights.
- `storage.bicep` — the storage account backing `AzureWebJobsStorage` and the Flex Consumption deployment container.
- `functionapp.bicep` — the Flex Consumption plan, the Function App itself (tagged `azd-service-name: host` so
  `azd deploy` can find it), a user-assigned managed identity, and the role assignments it needs
  (Storage Blob Data Owner on the storage account, Monitoring Metrics Publisher on Application Insights).

## Naming

Resources scoped to the resource group (Log Analytics, Application Insights, the managed identity, the App
Service plan) are named `{prefix}-{environmentName}` — no random suffix, since `rg-{environmentName}` already
gives them a unique scope. The storage account and Function App need to be unique across *all* of Azure (the
Function App's hostname is `*.azurewebsites.net`), so those two get a short hash (`resourceToken`, derived from
the subscription, environment name, and location) appended to keep them collision-safe.

## Provisioning

Run these yourself — they act on a real Azure subscription:

```powershell
azd auth login
azd provision   # creates the resource group, Function App, storage account, and Application Insights
azd deploy       # publishes host/src/Blog.Portfolio.Host to the provisioned Function App
```

`azd provision` is idempotent — re-running it against an existing environment reconciles in place rather than
duplicating resources. `azd env list` / `azd env select` manage multiple environments (e.g. separate dev/prod)
if you ever need one.

## CI/CD

Backend CI/CD is split into two workflows (ticket 06):

- `.github/workflows/backend-ci.yml` — runs on every commit that touches backend code. Builds, runs the full
  test suite, and publishes `host/` as the `host-publish` artifact.
- `.github/workflows/backend-cd.yml` — manually triggered (`workflow_dispatch`) only, for now. Downloads the
  `host-publish` artifact from the latest successful CI run on `main` (or a specific run via the `ci_run_id`
  input), runs `azd provision` to reconcile infra, then `azd deploy host --from-package` to deploy that exact
  artifact — no rebuild at deploy time. Finishes with a smoke test against the live `/api/example/ping`.

### One-time setup: federated credentials for GitHub Actions

`backend-cd.yml` authenticates to Azure via OIDC (no stored client secret). Run this once yourself, from a
machine with `azd` installed and logged in:

```powershell
azd auth login
azd pipeline config --provider github
```

This creates (or reuses) an Azure AD app registration with a federated credential trusting this repo's GitHub
Actions, and sets these as **repository variables** (Settings → Secrets and variables → Actions → Variables) —
none of them are secrets, since OIDC needs no client secret:

- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` — identify the federated app registration and
  target subscription.
- `AZURE_ENV_NAME`, `AZURE_LOCATION` — must match the `azd` environment you provisioned locally in the section
  above, so CD reconciles the same resource group rather than creating a second one.

If `azd pipeline config` sets any of these as **secrets** instead of variables, move them to repository
variables — `backend-cd.yml` reads them via `vars.*`.
