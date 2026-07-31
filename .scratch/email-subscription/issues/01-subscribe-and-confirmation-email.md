# 01 — Subscribe creates a Pending Subscriber and sends a real confirmation email

**What to build:** A visitor can submit their email address and receive a real confirmation email (via Resend) containing a confirm link and an unsubscribe link — even though clicking those links doesn't do anything yet (that's ticket 02).

**Blocked by:** None — can start immediately

**Status:** done

- [x] `apps/email-subscription/backend/{src,tests}` scaffolded following the `apps/example/` layout (REPR `Endpoint<TRequest, TResponse>` base class, one folder per endpoint) and referenced from `host/`
- [x] Subscriber persisted in Azure Table Storage: opaque GUID `Id`, `Email`, `Status` (Pending/Active/Unsubscribed); at most one record per email address
- [x] `POST /api/email-subscription/subscribe` creates a Pending Subscriber for a new email, reopens an Unsubscribed one back to Pending, and no-ops for an existing Pending/Active one (besides possibly re-sending the confirmation for Pending) — always returning the same generic response regardless of prior state
- [x] An optional honeypot field on the subscribe request causes a silent no-op (no record created or changed, same generic response) when populated
- [x] A stateless HMAC token utility signs `(subscriberId, purpose)` for "confirm" and "unsubscribe" purposes, usable both to generate and to validate signatures — this is shared with ticket 02
- [x] Subscribing enqueues a `{To, Subject, HtmlBody}` message onto an Azure Storage Queue, with the confirmation email body containing both a confirm link and an unsubscribe link (query params `id` and `sig`)
- [x] A queue-triggered function consumes that queue and sends the message via Resend
- [x] The Resend API key is a Function App setting (Bicep-templated), with a placeholder entry in `local.settings.json` for local dev
- [x] Bicep grants the Function App's managed identity the roles needed for Table Storage and Storage Queue access (alongside the existing blob role assignment)
- [x] The Host's CORS policy allows `https://www.sixsideddice.com`, `https://sixsideddice.com`, and a localhost dev origin
- [x] Unit tests cover subscribe's state-transition/idempotency logic (new/Pending/Active/Unsubscribed cases, honeypot no-op) and the send-email function's orchestration, with the Resend call mocked

## Comments

- CORS is enforced at the Function App resource level (`infra/functionapp.bicep` `siteConfig.cors`), not via ASP.NET Core middleware — the isolated worker's HTTP integration doesn't expose an `IApplicationBuilder` pipeline to hang `UseCors()` off. Local dev CORS uses the Functions Core Tools `Host.CORS` setting in `local.settings.json` instead.
- Full solution build and test suite pass (26 tests total, including the Aspire AppHost end-to-end test, which confirms the host starts cleanly with the new DI wiring). Reviewed via `/code-review` (Standards + Spec axes) with no hard violations; a few duplication smells were found and fixed (shared `SubscriberLinkAction`/`SubscriberLinkBuilder`, shared queue-name constant).
- Not verified against a real Azure deployment or a real Resend account — the Resend "from" domain still needs verifying in Resend itself, and `azd provision`/`azd deploy` are manual steps per ADR-0001's amendment. Both remain Lee's follow-up per the spec's Further Notes.
