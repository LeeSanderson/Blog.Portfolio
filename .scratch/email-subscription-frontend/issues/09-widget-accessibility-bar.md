# 09 — The widget's accessibility bar

Type: grilling
Status: resolved
Blocked by: —

## Question

Graduated from the map's fog once [Prototype the article widget](02-prototype-the-article-widget.md)
fixed the presentation. It could not be asked before: the answer depends on whether the widget
occludes the page, animates in, and steals focus — and all three are now settled (it does not
occlude, it has exactly one transition, and it never appears unbidden).

Scope is the **widget only**. The three standalone pages have their own accessibility questions —
notably what they show with JavaScript disabled — but those depend on
[Prototype the three subscription pages](06-prototype-the-subscription-pages.md) and stay in the fog
until it resolves.

Settle:

- **The result region.** The success, error and `pendingNote` messages all replace content in place.
  Whether that region is `aria-live="polite"`, `role="status"`, or both, and whether the error needs
  `role="alert"` instead — an error the reader caused by pressing a button is arguably not an alert.
- **Focus on collapse.** "Not now" and "I'm already subscribed" destroy the button that was just
  activated and replace the block with one line. Where focus lands: the collapsed line (made
  focusable), the next heading, or nowhere at all — and whether the collapsed line announces.
- **Focus on submit.** Same question for the success swap, which also destroys the submit button.
  Whether the live region alone is enough or focus has to move.
- **The heading level.** The widget injects a heading into someone else's document outline, between
  an `h2`–`h6` and whatever precedes it. What level it takes so the outline stays legible, given the
  anchor may be any of `h2` through `h6` and the injected heading sits *before* it.
- **The honeypot.** `SubscribeRequest` carries a `website` field the prototype hides off-screen.
  Whether `aria-hidden="true"` plus `tabindex="-1"` is the right treatment, or whether a
  screen-reader user can still land in it.
- **`prefers-reduced-motion`.** Already decided in ticket 02 — the 200ms collapse is disabled. This
  ticket only confirms nothing else needs the branch.

Consult `/grilling`. No prototype needed; the presentation is already fixed.

## Answer

Three things decided this ticket more than ARIA did. The widget is a **guest in someone else's
document**, so every id, heading level and landmark it injects is a change to a document it does not
own. It **ships no CSS at all** — light DOM kills `static styles`, established by
[Vite build shape](04-vite-build-shape.md) — so no answer may depend on a stylesheet. And a false
positive on the honeypot is **silent and unrecoverable**: `SubscribeFunction:41` returns the *same*
success message for a filled `website` field and sends nothing, so the widget writes `submitted` and
tells the reader to check an inbox that will never receive anything.

Two questions the ticket did not list were surfaced and settled: the ids the widget injects, and what
happens to a malformed address. Both are squarely the accessibility bar, and both were undecided.

### Measured, not assumed

The anchor rule from [Prototype the article widget](02-prototype-the-article-widget.md) was run
against all 27 published articles to find what level the anchor actually is:

| Anchor level | Articles |
|---|---|
| `h2` | 16 |
| `h3` | 8 |
| `h4` | 1 |
| no heading — end-of-`<main>` fallback | 2 |

So the anchor is deeper than `h2` on **9 of the 25** articles that have headings. That number is the
whole of the heading-level argument; without it the choice is taste.

### Structure — an `<aside>` with a fixed `h2`

The block is an **`<aside>` named by its own heading** via `aria-labelledby`. It is a plain
`<div data-pagefind-body>` inside `<main role="main">` that the widget injects into — `main` is not
sectioning content — so `aside` maps to `complementary` whether or not it is named. The landmark
earns its place by giving a screen-reader user one keystroke past a ~250px promo on every one of 27+
posts, and it makes the not-part-of-the-article claim programmatically.

The heading is a **fixed `h2`**, not the anchor's level. This skips a level on those 9 articles and
reparents the anchor heading underneath the signup block. Both were weighed and accepted:

- Skipping a level is **not a WCAG failure** — 1.3.1 does not require sequential headings. It is
  axe's `heading-order` rule, tagged best-practice.
- A top-level heading is the honest structure for a block that is not part of the article, and the
  `aside` bounds the false nesting by announcing itself as an aside.

The rejected alternative was matching the anchor's level at runtime, which produces no skip and keeps
the anchor's parent correct. It was not chosen; the fixed level is the simpler markup and the defect
it carries is a best-practice flag, not a barrier.

### Ids — namespaced, because the namespace is shared

The widget injects two ids into a document whose ids are BlogToHtml's slugified heading text
(`id="conclusion-a-sober-prediction"`). A collision on the input's id silently rebinds `<label for>`
to the wrong element and leaves the field unlabelled — a WCAG failure with no symptom.

Both are prefixed **`six-sided-signup-`**, matching the `six-sided.signup` storage key from
[Name the reader's local subscription state](01-name-the-readers-local-state.md), so the widget owns
one namespace in the document and in storage. A collision would need an article heading that reads
"six sided signup email", and only one widget ever mounts per page. Generated ids were rejected as
markup no test can assert a value on.

### One polite region, and nothing assertive

**A single `role="status"` region**, carrying `Sending…`, the network error and the invalid-email
message. `role="status"` implies polite and atomic, so `aria-live` alongside it is redundant.

Nothing in the widget is ever assertive. Assertive is for changes the reader did not cause, and every
message here answers a button they just pressed and are waiting on.

Two constraints rather than choices, both easy to get silently wrong:

- **The region is present and empty from first render.** A region Lit renders conditionally is created
  and populated in the same frame and is reliably silent.
- **`pendingNote` is not in this region and announces nothing.** It is the block a reader sees on a
  *later* article when their record is `submitted` — rendered at load, not a change. The ticket's own
  framing lumped it in with the live messages; that was wrong.

### Focus — one rule for all three destroying actions

Success, "Not now" and "I'm already subscribed" all destroy the control the reader was standing in.
All three get the same treatment: **the replacement takes `tabindex="-1"` and is focused with
`preventScroll: true`, and is announced exactly once by the focus move rather than by a live region.**
Focusing an element that is *also* a live region double-announces in some AT, so the success and
collapsed messages are focus targets and not live regions.

The alternative — no focus move, announce politely — loses focus to `<body>`, sending the reader's
next Tab to the blog header at the top of a 3,000-word article.

**Focus moves on click, never on `transitionend`.** Under `prefers-reduced-motion` the collapse
transition is disabled, `transitionend` never fires, and focus would silently never move: the branch
that exists for accessibility would break accessibility.

### The in-flight window — `aria-disabled`, not `disabled`

Both prototypes set `button.disabled = true`. Disabling the element that currently has focus drops
focus to `<body>` in every major browser. The success path self-heals because focus moves anyway; the
**error path does not** — the form stays, and the reader is told to try again while standing at the
top of the document with the button unreachable.

So the button keeps its native focusability: `aria-disabled="true"` plus an early return in the submit
handler. The look is free — the vendored Bootstrap already has
`.btn.disabled{opacity:.65}` and `.btn.disabled{pointer-events:none}`, which blocks the mouse; the
guard blocks keyboard re-entry.

### The honeypot — `hidden`, not off-screen

`<input type="text" name="website" hidden autocomplete="off">`.

The prototype's `.proto-honeypot` class comes from a prototype stylesheet with no counterpart in the
real widget, which ships no CSS. That leaves the hiding to travel with the markup or to depend on the
blog's stylesheet — and if the blog's CSS fails to load, an off-screen honeypot becomes a **visible,
unlabelled text box between the email field and Subscribe**, on a form where filling it means being
told you subscribed when you did not.

`hidden` is absent from the a11y tree and the tab order by definition, so it needs neither
`aria-hidden` nor `tabindex="-1"` — and it therefore avoids axe's `aria-hidden-focus` review flag,
which is what `aria-hidden` on a `tabindex="-1"` element raises. It is skipped by autofill and still
submitted with the form.

The cost, accepted: a bot that checks computed visibility skips it. Such a bot skips an off-screen
field too, so little is given up.

### Invalid input — `novalidate`, through the same region

The two prototypes disagreed: the widget's relied on native `type="email" required`, the pages'
set `novalidate` and never decided what replaced it. They cannot disagree — ticket 06 settled that
**one** shared form component mounts on both surfaces.

**`novalidate`, with `aria-invalid="true"` on the input and the message in the existing
`role="status"` region.** Validation and network failure then speak through one mechanism, themed
like every other message in the block, and persist until fixed. Native bubbles were rejected: light
on a Darkly page, self-dismissing in ~5s, wording not ours, screen-reader announcement inconsistent
across browsers, and the one message in the widget no test could assert on.

A dedicated inline error wired with `aria-describedby` was rejected as a second error surface and a
third injected id for a form with one field.

**Ticket 02's copy table gains a row:** *That doesn't look like an email address.*

### States

| State | Markup change | Announced by | Focus |
|---|---|---|---|
| prompt | initial render | — | untouched |
| in-flight | button gains `disabled` class + `aria-disabled="true"` | `role="status"` → "Sending…" | stays on the button |
| invalid email | `aria-invalid="true"` on the input | `role="status"` → *That doesn't look like an email address.* | untouched |
| network error | button re-enabled | `role="status"` → existing error copy | stays on the button |
| success | form and outs removed, `h2` stays visible | the focus move | `<div tabindex="-1">` |
| collapsed | `h2` → `class="sr-only"`, body → one line | the focus move | `<p tabindex="-1">` |
| `pendingNote` | rendered at load | nothing | untouched |

The `h2` survives every state. It stays **visible** through the success swap, where it reads naturally
above "Check your inbox". It goes **`sr-only`** on collapse, because
[Prototype the article widget](02-prototype-the-article-widget.md) budgets the collapsed state at one
line (~40px) and a visible heading plus a line is ~80px. Keeping it in the DOM means
`aria-labelledby` still resolves, the landmark keeps the name it had, and no second name string has
to be maintained alongside the copy table — and it is exactly what the blog already does with every
article's `h1`.

### Two freebies from the vendored Bootstrap

- `[tabindex="-1"]:focus:not(:focus-visible){outline:0!important}` is already in its reboot, so both
  new focus targets show a ring for keyboard users and none for mouse users, with no CSS from us —
  which matters, because we have none to give.
- `.sr-only` and `.btn.disabled` likewise come from the blog's stylesheet, so every decision above is
  reachable in a widget that ships no styles.

### `prefers-reduced-motion` — confirmed as one branch

Nothing added here needs the branch. The 200ms collapse remains the only motion in the widget: there
is no `scroll-behavior: smooth` anywhere in the blog's CSS (`site.css`, `bootstrapdarkly.min.css`,
`Blog/site.css` all checked), the focus moves use `preventScroll: true`, and the success swap has no
motion by ticket 02's decision.

### Markup

```html
<aside aria-labelledby="six-sided-signup-heading">
  <h2 id="six-sided-signup-heading">Get new posts by email</h2>
  <p>At most one email a week, and only when something new goes up. No tracking, no other mail,
     unsubscribe in one click.</p>
  <form novalidate>
    <label class="sr-only" for="six-sided-signup-email">Email address</label>
    <input type="email" required id="six-sided-signup-email" name="email"
           autocomplete="email" placeholder="you@example.com">
    <input type="text" name="website" hidden autocomplete="off">
    <button type="submit" class="btn btn-info">Subscribe</button>
  </form>
  <p role="status"></p>
  <p>
    <button type="button">Not now</button>
    <span>·</span>
    <button type="button">I'm already subscribed</button>
  </p>
</aside>
```

### Carried forward

- **[The three pages' accessibility, and what they do with no JavaScript](13-pages-accessibility-and-no-js.md)
  inherits the shared form wholesale** — `novalidate`, the single polite `role="status"` region, the
  `hidden` honeypot, the `six-sided-signup-` id namespace, the `aria-disabled` busy state and the
  invalid-email copy. Its own result-region bullet is now only about the pages' three-way
  `awaitingClick` → `working` → result swap, not about the form. Its ticket has been amended.
- **The `<aside>` and the `h2` are widget-only.** `/subscribe/` owns its whole document, has its own
  `h1`, and needs no landmark to be skippable.
- **Nothing here reopens a closed decision.** The heading level, the landmark and the honeypot all sit
  inside presentation that ticket 02 fixed; the only amendment to a closed ticket is the copy row.
