# 09 — The widget's accessibility bar

Type: grilling
Status: open
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
