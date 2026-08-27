# 02 — Prototype the article widget

Type: prototype
Status: open
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
