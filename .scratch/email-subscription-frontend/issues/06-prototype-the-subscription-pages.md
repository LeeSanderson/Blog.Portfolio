# 06 — Prototype the three subscription pages

Type: prototype
Status: resolved
Blocked by: 02

## Question

The three standalone pages are the one place where nothing is inherited: they sit at
`/subscribe/` on the blog's domain but are built here, so they get no header, no footer and no
layout unless this repo provides them.

Blocked by [Prototype the article widget](02-prototype-the-article-widget.md) because the sign-up
form is one shared component — its look is decided there, and this ticket places it in a page
rather than redesigning it.

Build a throwaway page for each of the three, loading the vendored `bootstrapdarkly.min.css` and
`site.css`, and settle:

- **Chrome.** Whether the pages reuse the blog's `six-sided-header` / `six-sided-footer` by
  loading them from `https://www.sixsideddice.com/js/`, ship copies, or stand bare. Reusing them
  makes the pages feel part of the site and deepens the coupling already accepted at charting;
  standing bare makes them obviously a different thing.
- **Confirm page.** What it shows while the `GET confirm` request is in flight, on success, and on
  the 400 that a bad or tampered signature produces — remembering the reader clicked a link in an
  email and has no idea what a signature is.
- **Unsubscribe page.** The same three states, and what tone a successful opt-out takes given the
  charting decision to offer no re-subscribe affordance.
- **Sign-up page.** What it says around the form to justify its own existence next to the widget,
  and where it would be linked from.
- **Landing-page reality.** Both landing pages are reached with `?id=&sig=` in the URL. Decide
  whether that is left visible, and what the pages do when the query string is missing entirely —
  someone will navigate to `/subscribe/confirm/` bare.

Link the prototype as an asset. Use `/prototype`.

## Assets

- **Prototype**: branch `prototype/subscription-pages`, commits `09045a6` (capture) and `51cbeaf`
  (defaults pointed at the settled design), at `apps/email-subscription/pages-prototype/`. Three
  chrome treatments switchable via `?variant=A|B|C`, with page, state, outcome, auto-fire and
  query-stripping as independent selectors so every combination is judgeable under every chrome.
  Loads the blog's real `bootstrapdarkly.min.css`, `site.css`, `Blog/site.css` and
  `six-sided-header`/`six-sided-footer`. See its `README.md` to run. Deliberately not merged to `main`.

## Answer

**Chrome variant B wins — the pages wear the blog's full chrome** and are ordinary pages of the
site: `six-sided-header`, `container > main.pb-3`, `six-sided-footer`, exactly `search.html`'s
shell. The two rejected variants are on the prototype branch: a bare centred panel with no site
chrome (A) and a header-only focused-task page (C).

Two facts established from the backend before building reshaped what these pages are allowed to
say, and are constraints on the spec:

- **Links never expire, and both actions are unconditional idempotent writes** (ADR-0007,
  `SubscriberLinkAction.TryApplyAsync`). A 400 is *only* a malformed id, a bad signature, or an
  unknown subscriber. **No page may ever say a link has expired** — it is not a thing that happens.
  Clicking twice is harmless.
- **`SubscribeFunction` re-sends for a `Pending` subscriber and reopens an `Unsubscribed` one to
  `Pending`** (lines 54–63). So `/subscribe/` is the route back in after opting out, which is why
  the unsubscribe page needs no re-subscribe affordance of its own.

### Chrome — and how the header actually loads

The ticket offered three ways to get the header and footer: cross-origin from
`www.sixsideddice.com/js/`, ship copies, or stand bare. A fourth looked obviously better —
the pages deploy to `/subscribe/` on `leesanderson.github.io`, the *same origin* as `/js/header.js`,
so a plain root-absolute tag should work exactly as it does in `search.html`.

**It does not, and finding out why decided the mechanism.** Built against Vite 8.2.2:

| What was built | Result |
|---|---|
| `<script type="module" src="/js/header.js">`, no `public/` copy | **Build fails**: `Failed to resolve /js/header.js` |
| Same tag, with `base: '/subscribe/'` and a `public/js/` copy | Builds, emits `src="/subscribe/js/header.js"` — **rewritten** |
| `<script type="module" src="https://www.sixsideddice.com/js/header.js">` | Builds, emitted **untouched** |
| `createElement('script')` with `src = '/js/header.js'` | Builds, string emitted **untouched** |

The pages deploy into a subdirectory, so `base` must be `/subscribe/`, and Vite prefixes `base`
onto every root-absolute reference it can see. A declarative same-origin tag therefore *cannot*
reach the blog's live header — it always resolves into the deployed subdirectory. The "vendor a
copy into `public/js/`" option is the ship-copies answer wearing a disguise, and its usual
advantage (no runtime dependency on the blog) is void, since the blog is the origin serving the page.

**Chosen: the fully-qualified URL.** It is the only option that is both declarative and gets the
live header, and it behaves identically in dev, in a preview and in production — no
`import.meta.env` branch, no copy that can drift. The price is the production hostname baked into
the page HTML and a network dependency in local dev, both accepted. Note it is only *cross-origin*
in dev; in production it is same-origin, so no `Referer` question arises.

The first row is worth keeping: unlike the three silent build traps from
[Vite build shape](04-vite-build-shape.md), this one **fails the build loudly**. It cannot ship broken.

### Both landing pages require a click

**The confirm and unsubscribe pages render an `awaitingClick` state and fire only on the button.**
The charting assumption that they act on load is dropped.

ADR-0007 justifies `GET` on both mutating endpoints on the grounds that *"the thing embedded in the
email is a link to an HTML page, not the API call itself"* — so a link-prefetching scanner cannot
trigger a state change. **That reasoning only holds if the page does not fire on load.** Auto-firing
reintroduces exactly the objection the ADR set aside, for any scanner that renders JavaScript
(detonation sandboxes do).

The failure modes are asymmetric, and click-on-both is the only configuration with no
silently-wrong outcome:

| | Auto-fire | Click |
|---|---|---|
| Confirm | a scanner creates an Active subscriber who never consented — defeats double opt-in | one extra click, never wrong |
| Unsubscribe | a scanner silently removes a real subscriber, who just sees the emails stop | one extra click, never wrong |

The cost is a click on a link the reader already clicked. Accepted: it is one button, the page
says what it is for, and it keeps the ADR's stated security property true rather than merely
assumed.

### The pages write the reader's Signup Record

**Confirm-success writes `confirmed`; unsubscribe-success writes `optedOut`.** `/subscribe/` and
`/Blog/` are the same origin, so the pages and the widget share one `localStorage`.

Two things this fixes, neither of them in the ticket:

- **`optedOut` had no writer at all.** [Name the reader's local subscription state](01-name-the-readers-local-state.md)
  defined four states; the widget writes `submitted`, `dismissed` and `confirmed`. Nothing wrote
  `optedOut`, making it dead code — the same argument that justified the dismiss control in
  [Prototype the article widget](02-prototype-the-article-widget.md).
- **The widget contradicted readers who had just confirmed.** Sign up (record: `submitted`) →
  click confirm → open an article, and `promptDecision` returns `pendingNote`: *"Still waiting on a
  confirmation click — the link is in your inbox."* They are not. Writing `confirmed` on the confirm
  page silences it.

Same trust model as the widget's "I'm already subscribed" control, which already writes `confirmed`
on the reader's unverified word. It only helps when the email link is opened in the same browser
the blog is read in — often it will not be — but it never hurts.

### `?id=&sig=` stays in the address bar

**No `replaceState`.** Stripping it is theatre: the signature is a permanent bearer token that never
expires, lives in the email forever, and is in browser history the moment the link is clicked, so
removing it from the address bar revokes nothing. Meanwhile it costs something real — now that a
click is required, stripping on load means a refresh before clicking strands the reader with no
token and no route back but the email.

### Copy

Confirm and unsubscribe share a state machine — `awaitingClick`, `working`, `success`, `failure`,
`offline`, `noQuery` — with `offline` deliberately distinct from `failure`. Conflating a 400 with an
unreachable API is the trap: one means the link is no good, the other means nothing happened and
retrying will work.

| Page | State | Copy |
|---|---|---|
| Confirm | `awaitingClick` | **Confirm your subscription** / One click and new posts from sixsideddice.com will start arriving. / `[Confirm my subscription]` |
| | `working` | Confirming your subscription… |
| | `success` | **You're subscribed.** / New posts will land in your inbox — at most one email a week, and only when something new goes up. / `[Read the blog]` |
| | `failure` | **We couldn't confirm that link.** / Email apps sometimes break long links across two lines. Try clicking it again from the email, or copy the whole address into your browser. / `[Sign up again]` → `/subscribe/` |
| | `offline` | **We couldn't reach the server.** / You're not subscribed yet — nothing has been saved. Check your connection and try again. / `[Try again]` |
| | `noQuery` | **There's nothing to confirm here.** / This page finishes off a subscription when you arrive from the link in a confirmation email. / `[Subscribe to the blog]` |
| Unsubscribe | `awaitingClick` | **Unsubscribe from sixsideddice.com** / You'll stop getting the post email. You can't undo this from this page. / `[Yes, unsubscribe me]` |
| | `working` | Unsubscribing… |
| | `success` | **You've been unsubscribed.** / You won't get any more emails from sixsideddice.com. / `[Read the blog]` |
| | `failure` | **We couldn't unsubscribe you with that link.** / Email apps sometimes break long links across two lines. Try clicking it again from the email, or copy the whole address into your browser. |
| | `offline` | **We couldn't reach the server.** / You're still subscribed — nothing has changed. Check your connection and try again. / `[Try again]` |
| | `noQuery` | **There's nothing to unsubscribe here.** / This page takes you off the mailing list when you arrive from the unsubscribe link at the bottom of one of the emails. / `[Read the blog]` |
| Sign-up | `idle` | **Get new posts by email** / At most one email a week, and only when something new goes up. No tracking, no other mail, unsubscribe in one click. / form / *Posts are about software craftsmanship, .NET and Azure. The email is the post list for that week and nothing else.* |
| | `success` | **Check your inbox** / There's a confirmation link waiting. You're not subscribed until you click it. |
| | `error` | That didn't go through. Check your connection and try again. |

Three choices worth their reasoning:

- **The opt-out is flat.** No "sorry to see you go", which is the mildest form of asking someone to
  reconsider and is what charting rejected when it ruled out a re-subscribe affordance. One neutral
  link to the blog stays: they may still be a reader, and a link to read is not a link to resubscribe.
- **A failed unsubscribe offers retry guidance only.** No human escape hatch, because there is no
  monitored mailbox to point at — the from-addresses are `noreply@` and `updates@`, and the blog
  carries no contact address. Making the canonical from-address a real receiving mailbox was
  considered and **declined**; the residual risk is recorded on the map.
- **A failed confirm links to `/subscribe/` rather than inlining the form.** Unlike the unsubscribe
  case this reader wants *in*, so recovery is not a re-ask — but it costs one click rather than a
  second mount of the shared form. The recovery is also only partial and the copy does not overclaim:
  a re-send reuses the same subscriber id and the HMAC has no nonce, so it re-sends the *same link*.
  That fixes a deleted record or a rotated key; it will not fix a link the reader's email client mangled.

### Carried forward

- **The shared form component mounts on two surfaces, not four.** Charting's Surfaces row implied the
  form appears on all three pages; it does not. It mounts in the widget and on `/subscribe/`. Neither
  landing page carries it in any state. The charting row is amended in place on the map.
- **`/subscribe/` never suppresses itself.** Unlike the widget it ignores the Signup Record entirely
  and always shows the form — the reader navigated there deliberately. `promptDecision` is the
  widget's alone.
- **`base` must be `/subscribe/`**, established while resolving the chrome mechanism. It belongs to
  [Choosing the Vite build shape to commit to](12-vite-build-shape-decision.md) and to the spec: it
  is what rewrites root-absolute references, and getting it wrong 404s every hashed asset.
- **A new accepted risk**, recorded on the map beside the other coupling edges: rotating
  `SigningKey` invalidates every confirm and unsubscribe link ever sent, all at once, and the
  failure page now offers no human fallback.
