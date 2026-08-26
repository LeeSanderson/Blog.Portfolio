# 03 — Verify the sixsideddice.com sending domain in Resend

Type: task
Status: open
Blocked by: —

## Question

Nothing to decide here — but the local confirm and unsubscribe flows cannot be exercised at all
until this is done, and if it turns out to be blocked, the charting decision to use real Resend
sends has to be revisited in favour of a dev-only file-drop `IEmailSender`.

The backend spec recorded this as *"a one-time setup task outside this repo, not yet done"*.
`EMAIL_SUBSCRIPTION_FROM_ADDRESS` is already set to `noreply@sixsideddice.com` locally, so the
from-address side is settled; the domain behind it is not.

This is Lee's to do — the agent cannot reach the Resend dashboard or the DNS zone. Checklist:

1. Add `sixsideddice.com` as a sending domain in the Resend dashboard.
2. Publish the DKIM/SPF records Resend gives you into the `sixsideddice.com` DNS zone.
3. Wait for Resend to report the domain as verified.
4. Send one test email from `noreply@sixsideddice.com` and confirm it arrives, not least because
   a freshly verified domain with no reputation can still land in spam.

Record in the answer: whether verification succeeded, which DNS provider holds the zone, whether
the test mail landed in the inbox or in spam, and any per-day or per-month sending limit on the
account — the weekly digest sends one message per Active Subscriber, so a low cap is a fact later
tickets need.

If verification cannot be completed, say so plainly in the answer rather than leaving it open:
that outcome reopens the local-email decision and the fallback sender becomes a new ticket.
