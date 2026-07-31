# 03 — Weekly digest emails Active subscribers about new posts

**What to build:** Every Monday, Active subscribers receive a real digest email listing any new sixsideddice.com blog posts from roughly the last week, each with a personalized unsubscribe link — or no email at all if nothing new was published.

**Blocked by:** 01 (needs the Subscriber storage and send-email queue infrastructure), 02 (needs a way to produce an Active Subscriber to test the digest against)

**Status:** done

- [x] Timer-triggered function runs on a Monday 08:00 UTC schedule
- [x] Fetches `https://www.sixsideddice.com/Blog/rss.xml` and selects items whose `pubDate` falls within a rolling 7-day window measured from the run time
- [x] If no items fall in the window, no emails are sent
- [x] For every Active Subscriber, enqueues one personalized `SendEmail` message (reusing ticket 01's queue/function) listing each new post's title (linked) and RSS `description` teaser, plus a personalized unsubscribe link (reusing ticket 01/02's token scheme)
- [x] RSS fetch/parsing failures are logged (Application Insights) without retry — an accepted risk, not a crash loop
- [x] Unit tests cover the rolling-window post-selection logic and the zero-new-posts no-send case, with RSS fetching and the outbound send abstracted behind interfaces so they're mockable

## Comments

- No explicit try/catch around the RSS fetch: an unhandled exception in a Functions isolated-worker invocation is already caught and logged to Application Insights by the host itself, and this timer has no configured retry policy — so the "logged, not retried" requirement is satisfied by the platform's default behavior rather than duplicated in application code.
- The digest's signed unsubscribe link now goes through the same `Tokens/SubscriberLinkBuilder.cs` that `SubscribeFunction` uses (extracted during code review to remove duplicated link-building logic).
