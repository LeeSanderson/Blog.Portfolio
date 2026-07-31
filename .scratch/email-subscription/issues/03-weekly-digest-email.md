# 03 — Weekly digest emails Active subscribers about new posts

**What to build:** Every Monday, Active subscribers receive a real digest email listing any new sixsideddice.com blog posts from roughly the last week, each with a personalized unsubscribe link — or no email at all if nothing new was published.

**Blocked by:** 01 (needs the Subscriber storage and send-email queue infrastructure), 02 (needs a way to produce an Active Subscriber to test the digest against)

**Status:** ready-for-agent

- [ ] Timer-triggered function runs on a Monday 08:00 UTC schedule
- [ ] Fetches `https://www.sixsideddice.com/Blog/rss.xml` and selects items whose `pubDate` falls within a rolling 7-day window measured from the run time
- [ ] If no items fall in the window, no emails are sent
- [ ] For every Active Subscriber, enqueues one personalized `SendEmail` message (reusing ticket 01's queue/function) listing each new post's title (linked) and RSS `description` teaser, plus a personalized unsubscribe link (reusing ticket 01/02's token scheme)
- [ ] RSS fetch/parsing failures are logged (Application Insights) without retry — an accepted risk, not a crash loop
- [ ] Unit tests cover the rolling-window post-selection logic and the zero-new-posts no-send case, with RSS fetching and the outbound send abstracted behind interfaces so they're mockable
