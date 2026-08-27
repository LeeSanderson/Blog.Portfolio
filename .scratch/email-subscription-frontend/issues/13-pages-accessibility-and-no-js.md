# 13 — The three pages' accessibility, and what they do with no JavaScript

Type: grilling
Status: open
Blocked by: —

## Question

Graduated from the map's fog once
[Prototype the three subscription pages](06-prototype-the-subscription-pages.md) settled the chrome,
the click-to-act flow and the copy. The vague version — "the three pages' accessibility, in
particular what they show with JavaScript disabled" — is now sharp, because ticket 06's decisions
made the no-JS answer concrete and unusually bad.

Scope is the **three standalone pages only**. The widget's half is
[The widget's accessibility bar](09-widget-accessibility-bar.md); the two share vocabulary but not
answers, because the pages own their whole document where the widget is a guest in someone else's.

**With JavaScript off, all three pages are currently blank.** Every one of ticket 06's decisions
pushed in the same direction, and the sum was not weighed at the time:

- Chrome B loads `six-sided-header` and `six-sided-footer` as `<script type="module">` — no JS, no
  chrome, on pages that are otherwise empty.
- Click-to-act means both landing pages do nothing until a JS handler runs.
- The form POSTs via `fetch`.
- Lit renders client-side.

So a reader who clicks a confirmation link with JS disabled sees an empty page and has no way to
confirm, and no indication anything is wrong. Note the blog's own `search.html` has the same
property and ships anyway — the precedent may be that this is simply accepted.

Settle:

- **The no-JS floor.** Whether the pages ship server-rendered static HTML in the `.html` entry that
  JS then takes over (so a `<noscript>` reader at least sees a heading and an explanation), a bare
  `<noscript>` block, or nothing at all on the `search.html` precedent. This is the decision the
  other bullets depend on.
- **Whether `noQuery` is reachable without JS.** It is the one state that needs no API call and no
  token, so it is the cheapest thing to render statically — and it is also the state a reader who
  lands bare is most likely to hit.
- **The result region.** `awaitingClick` → `working` → `success`/`failure`/`offline` replaces the
  page's main content three times. Whether that region is `aria-live="polite"`, `role="status"`, or
  `role="alert"` for the failure and offline states, and whether the `working` state announces at all
  or is noise.
- **Focus after the action.** The button the reader just pressed is destroyed by the swap. Where
  focus lands — the new heading, the result region, or nowhere — and whether the answer differs from
  [The widget's accessibility bar](09-widget-accessibility-bar.md)'s, given these pages own their
  whole document and the widget does not.
- **Heading levels and landmarks.** Each page has exactly one `h1` inside the `main` that chrome B
  supplies, and the header/footer arrive asynchronously from a different origin. What the outline
  looks like before they land, and whether the late-arriving `<header>`/`<footer>` landmarks cause a
  reader mid-page to lose their place.
- **`<title>` per page.** All three are separate documents and a screen-reader user hears the title
  first. What each says, and whether it changes with state — a page that stays "Confirm your
  subscription" after confirming is arguably lying.

Consult `/grilling`. No prototype needed; `prototype/subscription-pages` already shows every state
under the settled chrome, and the no-JS behaviour can be judged by disabling JS against it.
