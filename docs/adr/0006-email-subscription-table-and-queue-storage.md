# Table Storage and Storage Queue for EmailSubscription persistence and events

The EmailSubscription app needs somewhere to persist Subscriber records, and an event mechanism to decouple sending emails (via Resend) from the endpoints that trigger them. We chose Azure Table Storage for Subscriber persistence and an Azure Storage Queue for the send-email event, both provisioned in the storage account the Host already has — no new Azure resource. A single generic queue-triggered function (`{To, Subject, HtmlBody}` message) sends every outbound email, used by both the confirmation flow and the weekly digest (one message per digest recipient).

This keeps cost and infra surface minimal, consistent with ADR-0001 and ADR-0004, for a single simple record type and a single producer/consumer relationship at low volume. The Resend API key is a plain Function App setting (with a `local.settings.json` placeholder for local dev), not an Azure Key Vault secret — this app doesn't yet have enough secrets, or a strong enough compliance need, to justify standing up Key Vault.

## Considered Options

- **Azure SQL / Postgres / Cosmos DB** instead of Table Storage — rejected as unnecessary cost and setup for one simple record type with only point-lookup and single-partition-scan query needs.
- **Service Bus / Event Grid** instead of a Storage Queue — rejected; both add a new paid resource and features (dead-lettering, topics, pub/sub fan-out) this single producer/single consumer relationship doesn't need.
- **Azure Key Vault** for the Resend API key — rejected for now; revisit once there are multiple secrets or a real compliance driver, same reasoning as the rest of this repo's intentionally minimal infra.
