# 11 — Testing the anchor rule without a layout engine

Type: grilling
Status: open
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
