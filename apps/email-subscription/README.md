# email-subscription

Backend-only app letting readers of sixsideddice.com opt in to a weekly digest email of new blog posts, via
double opt-in with no per-token storage. No frontend lives here — the blog's own pages (built separately) call
these endpoints via JavaScript. See `.scratch/email-subscription/spec.md` for the full spec, `CONTEXT.md` for
the **Subscriber** glossary entry, and `docs/adr/0006-*.md` / `docs/adr/0007-*.md` for the storage and
token-signing decisions.

## backend

- `POST /api/email-subscription/subscribe` — creates a Pending `Subscriber` (or reopens an Unsubscribed one)
  and enqueues a confirmation email. Idempotent and honeypot-protected; always returns the same generic
  response. See `Subscribe/SubscribeFunction.cs`.
- `GET /api/email-subscription/confirm?id=&sig=` — validates an HMAC signature and unconditionally sets the
  Subscriber to Active. See `Confirm/ConfirmFunction.cs`.
- `GET /api/email-subscription/unsubscribe?id=&sig=` — validates an HMAC signature and unconditionally sets the
  Subscriber to Unsubscribed. See `Unsubscribe/UnsubscribeFunction.cs`.
- A queue-triggered function (`Email/SendEmailFunction.cs`) sends every outbound email via Resend; both the
  confirmation flow and the weekly digest enqueue onto it rather than calling Resend directly.
- A Monday 08:00 UTC timer function (`WeeklyDigest/WeeklyDigestFunction.cs`) emails every Active Subscriber
  about posts published in the last rolling 7 days, pulled from the blog's own RSS feed; sends nothing if
  there's nothing new.

Subscriber records live in Azure Table Storage; the send-email event is an Azure Storage Queue — both in the
Host's existing storage account (no new Azure resource, per ADR-0006).
