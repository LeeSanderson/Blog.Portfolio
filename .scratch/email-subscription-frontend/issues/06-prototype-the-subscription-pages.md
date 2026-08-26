# 06 — Prototype the three subscription pages

Type: prototype
Status: open
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
