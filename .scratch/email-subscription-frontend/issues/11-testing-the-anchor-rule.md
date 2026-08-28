# 11 — Testing the anchor rule without a layout engine

Type: grilling
Status: resolved
Blocked by: —

## Question

Surfaced while amending the map's charting "Testing" row during
[Prototype the article widget](02-prototype-the-article-widget.md).

That row justified a pure `shouldPrompt()` seam on the grounds that happy-dom has no layout engine
and the scroll trigger needed one. Ticket 02 removed the scroll trigger — but replaced it with an
anchor rule that reads `offsetTop` on every heading and on the article body. **The layout dependency
did not go away; it moved**, and the seam that was designed to contain it no longer sits in front of it.

[What the widget does when there is no anchor](10-widget-with-no-anchor.md) has since narrowed what
gets measured: the box is **`<main>`**, not `[data-pagefind-body]`, and the whole rule reads one
container plus heading tags. The fallback branch to test is "no `h2`–`h6` in `<main>`", not "not an
article" — that case no longer exists in the widget's code.

Settle:

- **Where the seam goes now.** The obvious candidate is a pure
  `pickAnchor(headings, articleTop, articleHeight) → index` over plain `{ offsetTop }` records,
  leaving the DOM measurement in a one-line caller. Whether that is the right cut, or whether the
  measurement and the choice are better tested together against a stubbed layout.
- **Whether happy-dom can be made to serve here at all.** It reports zero for layout properties
  rather than throwing, which makes a naive test pass while measuring nothing — the worst outcome.
  Whether the tests must therefore never touch the measuring code, or whether stubbing `offsetTop`
  on fixture elements is honest enough.
- **What the "after load, remeasure" behaviour costs in tests.** Ticket 02 requires measuring after
  images settle. Whether that reflow path is tested, or accepted as untested wiring.
- **Whether this reopens the browser-test trade.** The map ruled Playwright and browser-mode out of
  scope, explicitly trading them away because a seam made the scroll trigger testable. That trade
  was made against a mechanism that no longer exists, so it deserves one honest look — while noting
  that reversing it is a scope change, not a ticket resolution.

Consult `/grilling`.

## Answer

The seam moves to a **pure `indexNearestMidpoint`**, the rule switches from `offsetTop` to
**`getBoundingClientRect().top`**, and the widget **mounts once on `window.load`**. Six small tests,
none of which touches an unstubbed layout read. The browser-test trade **stands**.

Everything below was settled by grilling against measurements taken during this ticket, not asserted.

### The evidence this rests on

happy-dom 20.11.12 was installed and the rule from
[What the widget does when there is no anchor](10-widget-with-no-anchor.md) run against a fixture
article, unstubbed:

```
chose               : a          ← the FIRST heading
inserted into main? : true
is it before a h2?  : H2
```

Every `offsetTop` reads `0` and `main.offsetHeight` reads `0`, so every heading ties at distance zero
and the first wins. A test asserting "the widget landed in `<main>`, before an `h2`" **passes green**
while the widget sits at the top of the article — the exact failure ticket 02 refused `h1` to avoid.
The ticket's fear is not a risk, it is the default.

Three more findings, all of which changed a decision below:

| Probe | Result | Consequence |
|---|---|---|
| `offsetTop` descriptor | `configurable` getter on `HTMLElement.prototype`; instance `defineProperty` shadows it and survives re-querying | stubbing is mechanically sound |
| `getBoundingClientRect` descriptor | `writable, configurable` method | plain assignment stubs it — better ergonomics than a getter |
| `img.complete` | `true` immediately, with `naturalHeight` `0` | any image-gating logic short-circuits and is never exercised |
| `document.images` | **`undefined`** | would throw in tests while working in browsers |
| `offsetParent` | `undefined` (not `null`) | `offsetTop`-based code is unexercised there in every sense |
| `window.load` / `readyState` | fires; goes `interactive` → `complete` | both gate branches are drivable |

And the blog's own markup, which decides whether "measure after load" is real:

- All **27** articles carry exactly one `hero-image`, **inside `<main>`**, above every heading.
- **0 of 33** images declare `width`/`height`. **0** use `loading="lazy"`.
- `.hero-image` is `max-width: 100%` and nothing else — no height, no `aspect-ratio`.
- Nothing in the article path is positioned: no `position: relative` in `site.css`, and none on
  Bootstrap's `.container`. The chain is `body > .container > main > [data-pagefind-body] > h2`.

A hero of height H shifts every heading down by H but the midpoint only by H/2, so each heading's
distance from the midpoint moves by **H/2** — about **4.4pp** on a 4,000px article with a 350px hero,
against the **4.3pp** median deviation ticket 02 measured across all 27 articles. **Measuring before
load can genuinely pick a different heading.** The absence of lazy loading is the good news: every
image fetches at parse, so `window.load` is a real terminal point rather than a scroll-dependent one.

### Where the seam goes

**A pure `indexNearestMidpoint(headingTops, articleBox) → index | null`.** The midpoint arithmetic
lives inside it, not in the caller.

```js
const headings = [...main.querySelectorAll('h2, h3, h4, h5, h6')]
const index = indexNearestMidpoint(
  headings.map(heading => heading.getBoundingClientRect().top),
  main.getBoundingClientRect())
index === null ? main.append(widget) : headings[index].before(widget)
```

The rule has five parts and the cut puts three on the DOM side (select, measure, place) and two on the
pure side (midpoint-and-nearest, the no-headings fallback). Both alternatives were weighed:

- **A pure seam alone** — no test touches a layout read, which is the strongest possible guarantee
  against the all-zeros trap. Rejected because it leaves the heading selector, the container choice and
  `.before()`-versus-`.append()` as untested glue, and those are where the likelier bugs are.
- **An element-level `chooseAnchor(main) → Element | null` alone** — the whole rule in one unit and a
  two-line caller. Rejected because every arithmetic edge case then needs a full stubbed DOM to
  express, where as numbers it is one line.

**Ties go to the earliest heading** — a document-order scan with strict `<`. It is what the natural
implementation does, and it favours the earlier break in the prose. Stated because a test asserts it.

Sketched as `anchor.js` (pure) beside `widget.js` (glue), with tests beside each; the real file layout
is the spec's call, not this ticket's.

### Measured with `getBoundingClientRect().top`

**This amends the rule as written in both [Prototype the article
widget](02-prototype-the-article-widget.md) and [What the widget does when there is no
anchor](10-widget-with-no-anchor.md)**, which each say `offsetTop`.

`offsetTop` is measured from `offsetParent`, not from `<main>`, so comparing `main.offsetTop` against a
heading's `offsetTop` is only valid while both share one `offsetParent`. Today they do — nothing in the
article path is positioned, so both resolve to `<body>` — which makes `offsetTop` **correct now and one
CSS declaration away from silently wrong**: a single `position: relative` on `.container`, `<main>` or
`[data-pagefind-body]` would leave `<main>` measured from `<body>` and the headings measured from that
wrapper, comparing two coordinate spaces with no error and no signal.

`getBoundingClientRect().top` is viewport-relative, so `offsetParent` plays no part at all. Every read
in one synchronous pass shares a scroll origin, so the differences are exact. It deletes the failure
class rather than guarding against it, costs nothing, and stubs more cleanly.

A third option — keep `offsetTop` but assert at runtime that `<main>` and the chosen heading report the
same `offsetParent`, degrading to the fallback when they disagree — was rejected as production code
existing only for a scenario that cannot be reached, which is the shape ticket 10 refused for the
`<main>`-less case.

### Mounting: once, on load

```js
export function mountWidget(doc = document) { /* select, measure, place */ }

doc.readyState === 'complete'
  ? mountWidget()
  : addEventListener('load', () => mountWidget(), { once: true })
```

**There is no reflow path, so the question of whether to test one dissolves.** No remeasure, no
`ResizeObserver`, no `img.complete`, no `document.images`.

The prose-shove objection that killed ticket 02's scroll trigger does not transfer. That objection was
about inserting *inside the viewport being read*; `load` fires while the reader is at the top of the
article and the anchor is at ~48%, so nothing being read moves. This also stays correct if the blog's
hero images ever gain dimensions — the measurement is simply already stable when `load` arrives.

Two alternatives were weighed and rejected:

- **Insert at `DOMContentLoaded`, remeasure on load, relocate if the winner changed.** Relocating *is*
  the prose-shove, this time with the reader possibly already looking at the block. It is also the only
  option needing image bookkeeping — which happy-dom cannot exercise (`img.complete` is `true`
  instantly) and where `document.images` would throw in tests while working in browsers.
- **Insert at `DOMContentLoaded` and accept the skew.** Discards ~4.4pp against a rule tuned to 4.3pp;
  it throws away exactly what ticket 02's 27-article measurement bought.

**The `readyState` branch is kept, and both branches are tested.** Being precise: a
`<script type="module">` blocks the load event, so it always executes before `load` — the branch is
unreachable for the script tag ticket 02 specified. It is kept anyway because unlike ticket 10's
`<main>`-less guard, which needed an unreachable *page*, this one guards a **one-attribute edit to a
line in `C:/Dev/Personal/Blog`**: add `async` to speed the page up and the widget silently never appears
on any post, with no signal here. That is the worst failure available to this widget, and the guard
actually prevents it.

### The tests — six, and why each exists

Mounting needs an exported entry point regardless, because a test cannot drive a top-level `load`
listener that has already fired. That is what makes the gate separable enough to test.

| Test | Catches |
|---|---|
| nearest wins (answer is neither first nor last) | the arithmetic |
| exact tie | the documented earliest-wins rule |
| one heading | the degenerate case returns it, not `null` |
| no headings → `null` | the fallback's input |
| one fixture, rects stubbed | the `h2`–`h6` selector, the container read, `.before()` vs `.after()`, and `null` → append to end of `<main>` |
| gate, two cases | `readyState 'complete'` mounts synchronously; `'interactive'` mounts on dispatched `load` |

**One test-design rule carries the whole fixture test.** It must stub rects on **every** heading
*including the `sr-only` `h1`*, with the `h1` at top `0`, and its expected anchor must be the **third**
heading. Both halves are load-bearing:

- A test that stubs only the elements it queried *with the same selector the implementation uses* shares
  the implementation's selector bug and passes. Stubbing the `h1` too means an implementation that lets
  `h1` through picks it and the test fails.
- The all-zeros failure is deterministic — it always picks the **first** heading. Expecting the third
  turns a broken stub from a vacuous pass into a loud failure. Vacuity is designed out by fixture
  choice, not hoped away.

### What this does not reopen

**The browser-test trade stands.** Vitest + happy-dom only; no Playwright, no browser-mode. The honest
look the ticket asked for: a browser would test that Chromium computes `getBoundingClientRect`
correctly, which was never the risk. The reachable bugs are selector choice, arithmetic and BlogToHtml
drift — and drift cannot be caught in CI either way, because the published articles live in another
repo, so a browser test would still run against a fixture we wrote. It buys a real layout engine over a
fixture, for a browser download in ticket 07's single linear job, a dev dependency and an ADR.

Note that **this bullet's stated justification on the map was wrong** and has been corrected: it read
"the scroll trigger is made testable by a seam instead", and there is no scroll trigger.

A hand-run measurement script over the real 27 articles — the tool that produced ticket 02's numbers —
was offered as a middle path and also declined.

**Markup drift gets no automated signal.** Ticket 10 already refused a `console.warn` or a guard for a
missing `<main>`; having accepted silent failure at runtime, a test-time signal for the same case would
be inconsistent. The surviving surface is small and stable — `<main>` in `_Layout.cshtml`, unchanged
since the blog's Bootstrap 4 days, plus `h2`–`h6` tags that come from markdown. Folding it into
[The drift signal for the vendored blog CSS](14-vendored-css-drift-signal.md) was considered and
rejected: that ticket is about *vendored copies*, where a committed checksum works, and this is a *live
contract read from another repo's output*, which no checksum can see.

### Residue for the spec

Not decisions — things [Write the spec and implementation tickets](08-write-the-spec-and-tickets.md)
must carry:

- **The BlogToHtml contract, stated beside the script line the spec already documents**, since that is
  the only place it can travel to the repo able to break it:

  ```
  Add to Article.cshtml (NOT _Layout.cshtml):
    <script type="module" src="https://www.sixsideddice.com/subscribe/widget.js"></script>

  The widget reads, and only reads:
    - <main>            its box, for the midpoint
    - h2..h6 in <main>  the anchor candidates
  Do not add `async`: the widget mounts on load.
  ```

- The anchor rule must be written with `getBoundingClientRect().top`, not `offsetTop`, wherever the
  spec restates it — tickets 02 and 10 both carry the old wording.
- `document.images` and `img.complete` are named as APIs the frontend must not use, with the reason:
  they are silently useless under happy-dom.
