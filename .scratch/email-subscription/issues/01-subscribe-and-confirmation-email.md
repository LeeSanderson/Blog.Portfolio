# 01 — Subscribe creates a Pending Subscriber and sends a real confirmation email

**What to build:** A visitor can submit their email address and receive a real confirmation email (via Resend) containing a confirm link and an unsubscribe link — even though clicking those links doesn't do anything yet (that's ticket 02).

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] `apps/email-subscription/backend/{src,tests}` scaffolded following the `apps/example/` layout (REPR `Endpoint<TRequest, TResponse>` base class, one folder per endpoint) and referenced from `host/`
- [ ] Subscriber persisted in Azure Table Storage: opaque GUID `Id`, `Email`, `Status` (Pending/Active/Unsubscribed); at most one record per email address
- [ ] `POST /api/email-subscription/subscribe` creates a Pending Subscriber for a new email, reopens an Unsubscribed one back to Pending, and no-ops for an existing Pending/Active one (besides possibly re-sending the confirmation for Pending) — always returning the same generic response regardless of prior state
- [ ] An optional honeypot field on the subscribe request causes a silent no-op (no record created or changed, same generic response) when populated
- [ ] A stateless HMAC token utility signs `(subscriberId, purpose)` for "confirm" and "unsubscribe" purposes, usable both to generate and to validate signatures — this is shared with ticket 02
- [ ] Subscribing enqueues a `{To, Subject, HtmlBody}` message onto an Azure Storage Queue, with the confirmation email body containing both a confirm link and an unsubscribe link (query params `id` and `sig`)
- [ ] A queue-triggered function consumes that queue and sends the message via Resend
- [ ] The Resend API key is a Function App setting (Bicep-templated), with a placeholder entry in `local.settings.json` for local dev
- [ ] Bicep grants the Function App's managed identity the roles needed for Table Storage and Storage Queue access (alongside the existing blob role assignment)
- [ ] The Host's CORS policy allows `https://www.sixsideddice.com`, `https://sixsideddice.com`, and a localhost dev origin
- [ ] Unit tests cover subscribe's state-transition/idempotency logic (new/Pending/Active/Unsubscribed cases, honeypot no-op) and the send-email function's orchestration, with the Resend call mocked
