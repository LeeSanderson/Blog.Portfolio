# 02 — Confirm and unsubscribe links update Subscriber state

**What to build:** The confirm and unsubscribe links embedded in the email from ticket 01 now actually work — clicking through (via the blog page's JS calling these endpoints) moves the Subscriber to Active or Unsubscribed.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `GET /api/email-subscription/confirm?id=&sig=` validates the HMAC signature and sets the Subscriber to Active unconditionally — no guard on the Subscriber's prior state
- [ ] `GET /api/email-subscription/unsubscribe?id=&sig=` validates the HMAC signature and sets the Subscriber to Unsubscribed unconditionally — no guard on the Subscriber's prior state
- [ ] An invalid or mismatched signature is rejected without changing any Subscriber state
- [ ] Both endpoints reuse ticket 01's token validation utility rather than reimplementing signature checking
- [ ] Unit tests cover: valid confirm, valid unsubscribe, invalid signature, cross-purpose signature reuse (a confirm signature presented to the unsubscribe endpoint or vice versa), and the unconditional-write behavior regardless of the Subscriber's current state (e.g. confirm after already Unsubscribed moves it to Active)
