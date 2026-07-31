# EmailSubscription — Blog Post Notification Signups

Status: ready-for-agent

## Problem Statement

Lee wants readers of sixsideddice.com to be able to opt in to hear about new blog posts by email, without building any new frontend infrastructure right now — the blog itself lives in a separate repo (`leesanderson.github.io`) and today has no way to capture or notify subscribers. There's no reusable, provider-backed way to send transactional/digest emails from the portfolio backend, no subscriber list of any kind, and no scheduled job that watches the blog's RSS feed for new content.

## Solution

Add a new backend-only app, `apps/email-subscription/`, to the monorepo: a small set of HTTP endpoints and background functions that let a visitor subscribe to blog-post notifications via double opt-in, confirm or unsubscribe via emailed links (validated with a stateless signature, no per-token storage), and receive a weekly digest email of new posts pulled from the blog's own RSS feed. Sending is decoupled from every trigger via one generic queue-triggered "send email" function talking to Resend. No frontend code is added to this repo — the blog's own pages (built separately) call these endpoints via JavaScript.

## User Stories

1. As a blog reader, I want to submit my email address on the blog to be notified of new posts, so that I don't have to check the blog manually.
2. As a blog reader, I want a confirmation email after subscribing, so that my subscription can't be created by someone else entering my address without my consent.
3. As a blog reader, I want to click a link to confirm my subscription, so that I start receiving the weekly digest.
4. As a blog reader, I want a link to unsubscribe in every email I receive from this list, so that I can opt out at any time without contacting anyone.
5. As a blog reader who re-subscribes after unsubscribing, I want to go through the same confirmation flow again, so that I'm not locked out just because I once opted out.
6. As Lee, I want subscribing to be idempotent regardless of the address's current state, so that a visitor can safely submit the form more than once without creating duplicate records or leaking whether an address is already subscribed.
7. As Lee, I want a weekly scheduled job that reads the blog's RSS feed and emails everyone who is Active about posts published in roughly the last week, so that subscribers hear about new content without me doing anything manually.
8. As Lee, I want the digest to simply not send when there are no new posts that week, so that subscribers don't get empty "nothing new" emails.
9. As Lee, I want confirm/unsubscribe links to point at pages on the blog itself (which call this backend via JS), so that recipients land on a page I control rather than a bare API response, without needing any new frontend project in this repo.
10. As Lee, I want confirm/unsubscribe validation to use a signature I can verify without a database lookup, so that the tokens embedded in emails never need per-token storage, rotation, or expiry bookkeeping.
11. As Lee, I want a basic honeypot field on the subscribe request, so that unsophisticated bots filling every form field don't create junk Subscriber records.
12. As Lee, I want subscriber data and the send-email queue to live in the storage account the Host already provisions, so that this app adds no new Azure resource or cost.

## Implementation Decisions

- New app `apps/email-subscription/` (backend-only, no `frontend/`), following the established `apps/{app-name}/backend/{src,tests}` layout and REPR (`Endpoint<TRequest, TResponse>`) base class convention. Routes live under `/api/email-subscription/...` per ADR-0001.
- Domain model: a **Subscriber** (opaque GUID `Id`, `Email`, `Status`: Pending/Active/Unsubscribed) — see `CONTEXT.md`. At most one Subscriber record per email address.
- `POST subscribe`: creates a Pending Subscriber for a new email, or resets an Unsubscribed one back to Pending; a Pending or Active resubmission is a no-op besides possibly re-sending the confirmation email for Pending. Always returns the same generic response regardless of prior state (no email enumeration). Accepts an optional honeypot field; if populated, silently no-ops without creating or changing any record.
- `GET confirm?id=&sig=` and `GET unsubscribe?id=&sig=`: validate an HMAC signature over `(subscriberId, purpose)` (ADR-0007), then unconditionally set the Subscriber to Active or Unsubscribed respectively — no guard on prior state, no expiry, no stored token (ADR-0007). Both are `GET` because the emailed link points at a page on the blog itself, whose JS reads the query string and calls these endpoints — not a raw link a mail client/scanner could prefetch and accidentally trigger.
- Every confirmation and digest email is sent via one generic queue-triggered function (Azure Storage Queue, message shape `{To, Subject, HtmlBody}`) that calls Resend — the only place that talks to the Resend API (ADR-0006). `subscribe` enqueues one message per confirmation email; the weekly digest enqueues one message per Active Subscriber.
- Weekly digest: a Timer-triggered function running Mondays 08:00 UTC, fetching `https://www.sixsideddice.com/Blog/rss.xml` and selecting items with `pubDate` within a rolling 7-day window from run time. If none, sends nothing. Each included post shows its title (linked) and RSS `description` teaser. Every digest email includes a personalized unsubscribe link. RSS fetch failures are logged (Application Insights) and otherwise unhandled — an accepted risk, not retried.
- Persistence: Azure Table Storage for Subscriber records; Azure Storage Queue for the send-email event — both provisioned in the Host's existing storage account, no new Azure resource (ADR-0006).
- Resend API key: a plain Function App setting (Bicep-templated, supplied via `azd env`/deploy secret), with a placeholder entry in `local.settings.json` for local dev. No Key Vault (ADR-0006). The "from" address requires a domain verified in Resend — a one-time setup task outside this repo, not yet done.
- CORS: the Host's existing CORS configuration is extended to allow `https://www.sixsideddice.com`, `https://sixsideddice.com`, and a localhost origin for local blog-frontend testing.
- No abuse protection beyond the honeypot field: no rate limiting, no CAPTCHA — acceptable for a low-traffic personal blog for now.

## Testing Decisions

- Follows the existing stack (xUnit, FluentAssertions, Moq) and REPR testability pattern established in `apps/example/`.
- `subscribe`/`confirm`/`unsubscribe` endpoint logic (state transitions, idempotency, HMAC validation) is unit-tested against the `Endpoint<TRequest, TResponse>.HandleAsync` seam, independent of the HTTP trigger binding, same as `PingFunctionTests`.
- The send-email queue function and the weekly digest function should have their Resend and RSS-fetching dependencies abstracted behind an interface so their orchestration logic (what gets sent, to whom, and when) is unit-testable without a real network call.
- No architecture-test changes needed — the existing route-prefix test already covers any new `/api/email-subscription/...` endpoint.
- No frontend tests, since this app has no `frontend/`.

## Out of Scope

- Any frontend code, in this repo or the blog's — the blog's own confirm/unsubscribe/subscribe-form pages are Lee's separate, manual follow-up work.
- Rate limiting, CAPTCHA, or any anti-abuse measure beyond the honeypot field.
- Confirm-link expiry, token rotation, or per-token storage/revocation.
- Multiple subscription topics/lists — this app only ever represents "subscribed to sixsideddice.com blog posts."
- Azure Key Vault, or any secret-management approach beyond a plain Function App setting.
- Retry/alerting for a failed weekly RSS fetch.
- Setting up the verified sending domain in Resend itself (an external, one-time account setup task).

## Further Notes

- Produced via a `/grill-with-docs` session; the domain glossary (**Subscriber**) lives in `CONTEXT.md`, and the storage/queue and token-signing rationale live in `docs/adr/0006-email-subscription-table-and-queue-storage.md` and `docs/adr/0007-hmac-signed-subscriber-tokens.md`.
- `apps/example/` remains the reference layout to copy for the endpoint/test folder structure.
