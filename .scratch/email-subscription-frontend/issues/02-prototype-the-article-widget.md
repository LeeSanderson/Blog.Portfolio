# 02 — Prototype the article widget

Type: prototype
Status: resolved
Blocked by: —

## Question

What does the widget actually look like and do, in an article, on a real Bootstrap Darkly page?

This is the ticket the whole effort hangs on: the presentation choice decides whether the widget
needs a DOM anchor at all, what the accessibility work is, and what the shared form component
looks like everywhere else.

Build something cheap and concrete to react to — a static HTML page that loads the blog's real
`bootstrapdarkly.min.css` and `site.css` with a real article's markup around it, with two or
three presentations side by side. Do not build it as the real component; this is throwaway.

Settle:

- **Presentation.** Inline block at the end of the article, a slide-up bar fixed to the viewport
  bottom, an inline block part-way through, or something else. Each implies a different anchor:
  end-of-article needs `<main>`, a fixed bar needs nothing but `<body>`.
- **Trigger.** What "scrolled down" means as a number — a fraction of article height, a distance
  from the end, or an element coming into view — and what `shouldPrompt(state, position)` takes
  as arguments so it stays a pure, unit-testable function with the observer wiring left trivially thin.
- **Copy.** What the prompt says, what the success state says after submitting, and what a reader
  mid-flow (`submitted`) sees instead of the prompt.
- **Dismissal.** Whether there is a visible dismiss control, what it looks like, and whether
  dismissing is distinguishable from ignoring.
- **The "I'm already subscribed" control.** Settled in
  [Name the reader's local subscription state](01-name-the-readers-local-state.md) as the mitigation
  for per-browser state: it writes a `confirmed` Signup Record locally and never calls the server.
  This ticket decides whether it is always visible, revealed only on a repeat prompt, or folded into
  the dismiss control — and what it says.
- **Motion.** Whether it animates in at all, and what `prefers-reduced-motion` does.

Link the prototype from this ticket as an asset rather than pasting it in. Use `/prototype`.

## Assets

- **Prototype**: branch `prototype/article-widget`, commit `4c4c2c8`, at
  `apps/email-subscription/frontend-prototype/`. Three variants on one page, switchable via
  `?variant=A|B|C`, inside a verbatim copy of
  `Blog/Thoughts/VibeCodingTheEmperorsNewClothes.html` with the blog's real
  `bootstrapdarkly.min.css`, `site.css`, `Blog/site.css` and `six-sided-header`/`six-sided-footer`.
  See its `README.md` to run. Deliberately not merged to `main`.

## Answer

**Variant C — a full-width interstitial breaking the prose — wins.** Hairline rules top and
bottom, centre-aligned, heading-led, no card chrome. The two rejected variants are on the
prototype branch: an end-of-article card (A) and a bar fixed to the viewport bottom (B).

Everything below was settled by grilling against the prototype. Where a decision contradicts
what was assumed at charting, that is called out.

### Anchor

Insert **before the `h2`–`h6` whose `offsetTop` is nearest the vertical midpoint of
`[data-pagefind-body]`**, measured after load so late-loading images do not skew the offsets.
No heading of any level → fall back to the end of `<main>`, which is variant A's placement.

The ticket's own suggestion — the 3rd `<h2>` — was measured across all 27 published articles
and abandoned:

| | |
|---|---|
| Articles with no `<h2>` at all | 2 (both K8s posts) — widget would never appear |
| Where the 3rd `<h2>` actually falls | 12.5% → 86.4% through the body, median 48.3% |

`Part5-StorageAccounts` has 16 `<h2>`s so the 3rd is 15% in; `GeneratingAStaticWebsiteUsingMarkdown`
has 4 so the 3rd is 86% in. Same rule, opposite behaviour, unpredictable from the rule.

Widening from `<h2>` to any heading level measurably improves placement — median deviation from
the midpoint falls from **10.2pp to 4.3pp**, because sub-headings fill the gaps that section
headings leave. Three consequences:

- **Never `h1`.** The article title sits inside `[data-pagefind-body]` as `class="sr-only"` at
  position 0. A literal "any header" rule would anchor there on the two K8s posts — which have no
  heading of any other level — putting the widget above the hero image at the top of the page and
  silently swallowing the end-of-`<main>` fallback.
- **The worst case is content, not rule.** `Part6-VirtualMachines` still lands at 83.6% because
  its last heading is at 83.6% with nothing between. It is genuinely one long section; no rule
  fixes it, and the block just sits late on that post.
- **The price is a softer pause.** An `h3`/`h4` is a subsection boundary, so on
  `DealingWithMissingData` the block lands at an `h4`. Accepted: better placement, weaker break.

### Trigger — there isn't one

**The widget is inserted at load and the reader scrolls into it.** The charting assumption of a
scroll trigger is dropped.

Building the prototype showed the trigger to be incoherent for an in-flow element. With the anchor
at ~48% and the trigger at 50%, insertion happens *inside the viewport being read*, shoving the
current paragraph down by the height of the block. Worse, the trigger buys nothing: its purpose
was to pitch only to engaged readers, but an element anchored at the midpoint is only *seen* by
readers who reached the midpoint. Trigger and anchor select the same people; the anchor is the one
doing real work.

Reserving the block's space at load and fading content in on intersection was considered and
rejected — it keeps the whole observer apparatus and adds a guessed height.

Two consequences:

- **The seam loses its position argument**: `promptDecision(record) → 'prompt' | 'pendingNote' | 'silent'`.
  Nothing in it touches layout.
- **It is `promptDecision`, not `shouldPrompt`.** A boolean cannot express three outcomes —
  prompt, say nothing, and acknowledge a submission already in flight.
- **The map's charting "Testing" row is now wrong** and has been amended in place: there is no
  scroll trigger, so nothing needs a seam to work around happy-dom's missing layout engine.

### Dismissal

**"Not now" → writes `dismissed` → the block collapses in place to a single line.**

The deciding argument is not taste: without a dismiss control nothing in the system can ever write
`dismissed`, and [Name the reader's local subscription state](01-name-the-readers-local-state.md)'s
deliberate 30-day ageing window becomes dead code. The case against — an in-flow block occludes
nothing, so you can just scroll past it — fails because that escape has to be re-exercised on every
one of 27+ posts, which is exactly the nagging the 30-day window was designed to end.

Collapse rather than vanish, because a block that disappears on click reads as a mis-click or a
bug, and because vanishing yanks ~250px of prose upward mid-sentence where collapsing to one line
costs ~40px. The collapsed line lives only for the rest of that page view; once `dismissed` is
written, `promptDecision` returns `silent` and the next article shows nothing at all.

### The "I'm already subscribed" control

**Always visible, a peer of "Not now", worded exactly "I'm already subscribed".** Writes a
`confirmed` Signup Record, calls nothing, and collapses through the same mechanism as dismiss with
a different sentence.

Revealing it only on a repeat prompt was rejected on two counts. It hides the control from the
reader it was built for — someone who confirmed on their phone is on a *first* prompt on their
laptop, not a repeat — and it needs an impression counter that the settled `{v, state, at}` shape
does not have, so it would mean reopening ticket 01's storage decision to buy progressive
disclosure.

Folding it into the dismiss control was rejected outright: `dismissed` ages out in 30 days and
`confirmed` never does, so two permanences behind one button is a trap.

The wording matters because this control writes `confirmed` on the reader's unverified word. It
should read as a claim the reader is making, not a badge being shown them — "Already subscribed"
reads like a status label, "I'm already subscribed" like an assertion.

### Motion

**The collapse only: ~200ms height ease-out, disabled under `prefers-reduced-motion`. Nothing on
the success swap.**

Dropping the trigger dropped the slide-in with it. The collapse earns its 200ms because 250px → 40px
is a hard yank with the reader's eye on the exact spot, and the animation is the difference between
"that collapsed" and "the page moved". The success swap does not: the reader has just pressed a
button and is watching it, so an instant result is the most reassuring outcome available, and its
height change is smaller.

Animate by measuring `scrollHeight`, setting an explicit pixel height, transitioning to the
measured collapsed height, then clearing — about ten lines and no browser-support question.
`interpolate-size`/`calc-size()` was deliberately not used; its current cross-browser support was
not verifiable while resolving this ticket and is not worth a research ticket for one transition.

One transition in the whole widget means one `prefers-reduced-motion` branch.

### Copy

**The widget renders one fixed success message for any 200 and never inspects the response's
`message` field.** `SubscribeFunction` returns 200 with a fixed hedged sentence — *"If that address
isn't already subscribed, check your inbox for a confirmation email."* — because an API cannot know
how its consumer will render. The widget does know, so it writes its own copy. The anti-enumeration
property is preserved by construction: the leak was only ever in *differential* responses, and an
unconditional message differs in nothing.

The cost, accepted: for an already-Active address `SubscribeFunction` no-ops and sends nothing, so
"check your inbox" is mildly untrue for that reader. They are now the rare case and they have a
dedicated control, and the alternative makes every reader parse a conditional to protect them.

| Slot | Copy |
|---|---|
| Kicker | *none* — the block already interrupts; a kicker begs on top of interrupting |
| Heading | Get new posts by email |
| Body | At most one email a week, and only when something new goes up. No tracking, no other mail, unsubscribe in one click. |
| Button | Subscribe |
| Success | Check your inbox — there's a confirmation link waiting. You're not subscribed until you click it. |
| `submitted`, later article | Still waiting on a confirmation click — the link is in your inbox. |
| Error | That didn't go through. Check your connection and try again. |
| Collapsed after "Not now" | No problem — we won't ask again for a while. |
| Collapsed after "I'm already subscribed" | Thanks — we won't ask again in this browser. |

Two choices worth their reasoning. The heading states the offer flatly rather than warmly, because
a scanning reader reads only the heading and warmth belongs in the body line. The success message
spends its second sentence on the confirm click because a Pending subscriber who never clicks is
the one failure mode ticket 01 identified as silent and unrecoverable.

**"One email a week" would have been an overpromise.** `WeeklyDigestFunction:43` returns early when
the seven-day window holds no posts, so a quiet week sends nothing. The body line says *at most*
one, and only when something new goes up.

### Carried forward

- **The form's shared shape is now fixed** by what the prototype needed: an `email` input, a
  hidden `website` honeypot (`SubscribeRequest` takes one), a submit button, and a status region.
  The three pages inherit it.
- **A new coupling edge**, recorded on the map beside the light-DOM one: the anchor rule reads
  BlogToHtml's `data-pagefind-body` wrapper and its heading markup, so a template change in
  `C:/Dev/Personal/Blog` can silently move or remove the widget with no signal in this repo and no
  test here to catch it.
