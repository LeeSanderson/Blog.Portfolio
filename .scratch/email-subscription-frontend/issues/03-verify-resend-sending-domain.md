# 03 — Verify the sixsideddice.com sending domain in Resend

Type: task
Status: resolved
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

## Answer

**Done. `sixsideddice.com` is a verified sending domain in Resend.** The charting decision to use
real Resend sends locally stands; the dev-only file-drop `IEmailSender` fallback is not needed and
should not be built.

The four facts the ticket asked for:

| | |
|---|---|
| Verification | Succeeded |
| DNS zone | 123-reg — the registrar's own DNS, where the DKIM/SPF records were published |
| Test mail | Landed in the **inbox**, first send, from `noreply@sixsideddice.com` |
| Sending cap | Resend **free tier**: 100/day, 3,000/month |

### Local dev is wired end to end

Checked in the repo while resolving this, because "the domain is verified" alone doesn't prove the
loop runs: `host/src/Blog.Portfolio.Host/local.settings.json` (untracked, as intended) carries a real
`RESEND_API_KEY` and `EMAIL_SUBSCRIPTION_FROM_ADDRESS=noreply@sixsideddice.com`, on the
now-verified domain. `./run-local.ps1` therefore exercises the whole confirm/unsubscribe loop against
real mail with no further setup. **This is the last thing that was gating local end-to-end testing of
the backend flows the frontend calls.**

### Two facts later work depends on

**The from-address differs between local and deployed.** Local sends from `noreply@sixsideddice.com`;
`infra/main.parameters.json` defaults to `${EMAIL_SUBSCRIPTION_FROM_ADDRESS:=updates@sixsideddice.com}`,
so a deploy with the repo variable unset sends from `updates@`. Both addresses are on the verified
domain, so neither is broken and nothing is blocked — but the two environments send as different
people. Resolving it is one repo-variable setting, not a decision; the spec should state which address
is canonical rather than leave the fallback to decide.

**100/day is the binding cap, and it binds the digest, not this frontend.** The weekly digest enqueues
one message per Active Subscriber and they all go out on the same day, so the daily cap — not the
monthly one — sets the ceiling: **roughly 100 Active Subscribers before a digest send starts failing**,
with 3,000/month leaving comfortable headroom underneath it. Nothing on this map trips it. Signup
confirmations are one mail per signup, orders of magnitude below 100/day at this blog's scale, and the
frontend never sends anything itself. Recorded here so the fact is findable, and ruled out of scope on
the map rather than ticketed — see the map's Out-of-scope entry.
