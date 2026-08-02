# infra/

Bicep for the shared Azure Function App (`host/`), its storage account, and Application Insights, deployed via
the Azure Developer CLI (`azd`). See ADR-0004 for why this is hand-authored rather than generated from the
Aspire AppHost model.

- `main.bicep` — subscription-scoped entry point: creates the resource group, wires up the modules below.
- `appinsights.bicep` — Log Analytics workspace + Application Insights.
- `storage.bicep` — the storage account backing `AzureWebJobsStorage`, the Flex Consumption deployment
  container, and the `email-subscription` app's `Subscribers` table and `send-email` queue (see
  `docs/adr/0006-email-subscription-table-and-queue-storage.md`).
- `functionapp.bicep` — the Flex Consumption plan, the Function App itself (tagged `azd-service-name: host` so
  `azd deploy` can find it), a user-assigned managed identity, the role assignments it needs (Storage Blob Data
  Owner, Storage Table Data Contributor, and Storage Queue Data Contributor on the storage account, Monitoring
  Metrics Publisher on Application Insights), CORS for the `sixsideddice.com` blog frontend, and the
  `email-subscription` app's Resend/signing-key settings.

## email-subscription secrets

`resendApiKey` and `emailSubscriptionSigningKey` are `@secure()` params with no default, sourced from azd
environment variables via `main.parameters.json`. Set them once per environment before provisioning:

```powershell
azd env set RESEND_API_KEY <value> --secret
azd env set EMAIL_SUBSCRIPTION_SIGNING_KEY <value> --secret
```

`emailSubscriptionFromAddress` is parameterised the same way but isn't a secret, so its `main.parameters.json`
entry uses the `${EMAIL_SUBSCRIPTION_FROM_ADDRESS:=updates@sixsideddice.com}` form — falls back to
`updates@sixsideddice.com` (matching the Bicep param default) when the azd environment variable is unset or
empty, so you only need to set it if an environment should send from a different address:

```powershell
azd env set EMAIL_SUBSCRIPTION_FROM_ADDRESS <value>
```

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

### App secrets for the Function App (e.g. `RESEND_API_KEY`)

Unlike the `AZURE_*` identifiers above, the app's own secrets (currently `RESEND_API_KEY` and
`EMAIL_SUBSCRIPTION_SIGNING_KEY`) are real secrets and must be added as **repository secrets** (Settings →
Secrets and variables → Actions → Secrets), not variables. `backend-cd.yml`'s "Provision infrastructure" step
reads them via `secrets.*` and exposes them as the environment variables that `main.parameters.json` resolves
(`${RESEND_API_KEY}`, `${EMAIL_SUBSCRIPTION_SIGNING_KEY}`) when it runs `azd provision`. The non-secret
counterpart, `EMAIL_SUBSCRIPTION_FROM_ADDRESS`, follows the same wiring but as a **repository variable** read
via `vars.*` — leave it unset in repos/environments that are happy with the default `updates@sixsideddice.com`.

`.azure/` is gitignored, so CI never has the local azd environment created by `azd env set` on your machine —
each CD run resolves these parameters purely from the repository secrets/variables above. `azd provision` still
reconciles the same resource group as your local environment because `AZURE_ENV_NAME` (and the naming derived
from it) is unchanged, not because any local state carries over.

To add a new app setting for the Function App in future (secret or not), wire it through all four layers:

1. **Bicep**: add a param to `functionapp.bicep` (and thread it through `main.bicep`), then add an entry for it
   in the `appSettings` array in `functionapp.bicep`. Use `@secure()` for anything sensitive.
2. **`main.parameters.json`**: if it's sourced from an azd environment variable rather than a fixed default, add
   `"paramName": { "value": "${ENV_VAR_NAME}" }`. If the Bicep param already has a sensible default and the
   setting is genuinely optional, use the `${ENV_VAR_NAME:=default value}` form instead of a bare `${ENV_VAR_NAME}`
   — see the gotcha below for why the colon matters.
3. **Local**: `azd env set ENV_VAR_NAME <value>` (add `--secret` for sensitive values) so `azd provision` can
   resolve it locally.
4. **CI/CD**: add `ENV_VAR_NAME` as a repository variable (non-secret) or secret (sensitive), then expose it via
   `vars.*`/`secrets.*` as an env var on the "Provision infrastructure" step in `backend-cd.yml`, matching the
   pattern above.

**Gotcha with optional settings in CI**: `${ENV_VAR_NAME}` (no default) resolves to an empty string if the
variable is unset anywhere azd looks — it does not fall back to the Bicep param's default, so never use the bare
form for an optional setting. Worse, `${ENV_VAR_NAME=default}` (no colon) only falls back when the variable is
*completely unset* — but `vars.*`/`secrets.*` in a GitHub Actions `env:` block always sets the env var, to an
empty string when the repository variable/secret doesn't exist. That combination means an unconfigured optional
repository variable would silently deploy an empty string instead of the intended default. Use the colon form,
`${ENV_VAR_NAME:=default}`, which falls back on empty *or* unset — this is what `emailSubscriptionFromAddress`
does above.
