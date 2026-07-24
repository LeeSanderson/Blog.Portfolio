# Infrastructure as code via Aspire + azd from day one

Azure resources (the Function App, its storage account, Application Insights, and any per-app resources) are provisioned as Bicep and deployed with the Azure Developer CLI (`azd`) — rather than creating the resources manually once, outside the repo.

This is more setup than strictly necessary for what is currently a single, long-lived resource. It was chosen deliberately so the infrastructure can be expanded or recreated easily as new apps, and their supporting Azure resources, are added over time.

## Amendment: Bicep is hand-maintained, not AppHost-generated

The original intent was for `azd infra gen` to synthesize the Bicep directly from the Aspire AppHost model, so the IaC would stay in lockstep with `aspire/Blog.Portfolio.AppHost/AppHost.cs` automatically. In practice (`Aspire.Hosting.Azure.Functions` 13.4.6, the latest version at time of writing), `AddAzureFunctionsProject` only wires up local-dev orchestration (the Azurite emulator) — it has no publish-time method of its own. `azd`'s generic "any project resource becomes a container" fallback is what actually runs, which deploys the Functions worker inside an Azure Container App (always-on billing, no native Function App resource, no Application Insights) rather than a real Function App on a Flex Consumption plan.

That doesn't meet this ADR's goal, so `infra/*.bicep` is hand-authored instead: a Flex Consumption Function App, its storage account, and Application Insights, wired together in `infra/main.bicep`. `azd` still does the provisioning/deployment orchestration (`azure.yaml` declares `host` as a `host: function` service) — only the Bicep authoring step is manual rather than generated. Revisit this once Aspire's Azure Functions integration adds a genuine Flex Consumption publish target.
