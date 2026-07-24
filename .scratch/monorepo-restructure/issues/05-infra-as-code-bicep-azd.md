# 05 — Infrastructure as code (Bicep via Aspire + azd)

**What to build:** Real Azure infrastructure for the shared Function App, provisioned as Bicep and deployed via `azd`, per ADR-0004 — chosen deliberately so infra can be expanded or recreated easily as apps are added.

**Blocked by:** 04

**Status:** ready-for-human

- [x] An `infra/` folder contains Bicep for the Function App (Flex Consumption), its storage account, and Application Insights — see `infra/README.md`. Hand-authored rather than generated from the Aspire AppHost model; see the ADR-0004 amendment for why.
- [x] An `azure.yaml` exists at the repo root describing the deployment targets for `azd`
- [ ] Running `azd provision` (or equivalent) successfully creates the Azure Function App, its storage account, and Application Insights in Lee's Azure subscription — **left for Lee**: the agent has no Azure credentials/subscription access in this environment. Commands are documented in `infra/README.md`.
- [ ] The provisioned resources are confirmed to exist (e.g. via Azure Portal or `az` CLI) after provisioning — **left for Lee**
- [ ] Re-running provisioning is idempotent (no errors, no duplicate resources) — supports the "expand or recreate infra easily" goal from ADR-0004 — **left for Lee** to confirm once provisioned once

## Comments

- Attempted `azd infra gen` against the real Aspire AppHost first, per the original plan. It only produces infra for Azure Container Apps (the Functions worker running in a container) — `Aspire.Hosting.Azure.Functions` 13.4.6 (latest on NuGet) has no native Function App publish target, `AddAzureFunctionsProject` is local-dev-orchestration only. Confirmed with Lee: hand-author the Bicep instead (Flex Consumption Function App, Storage, Application Insights), keep `azd` for provisioning/deploy orchestration. See the ADR-0004 amendment.
- `infra/main.bicep` compiles via `az bicep build` (offline, no Azure calls) with 0 errors and one benign warning (BCP334 on the storage account name interpolation — Bicep can't statically prove `uniqueString()`'s length, but it's always 13 chars, well over the 3-char minimum). Not run against a real subscription — see above.
