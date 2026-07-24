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
