# 10 — What the widget does when there is no anchor

Type: grilling
Status: resolved
Blocked by: —

## Question

Graduated from the map's fog ("Non-article pages") once
[Prototype the article widget](02-prototype-the-article-widget.md) settled the anchor rule. The
vague version — "what if the script is loaded somewhere without an article" — is now a sharp
question about two specific failure shapes.

Checked rather than assumed, across `C:/Dev/Personal/leesanderson.github.io`:

| Page | `<main>` | `[data-pagefind-body]` |
|---|---|---|
| Every article under `Blog/` | yes | yes |
| `index.html`, `search.html`, `404.html`, `Blog/index.html`, `Blog/all.html` | **yes** | no |

So `<main>` is on every page and is *not* a usable "is this an article?" test, while
`[data-pagefind-body]` is exactly the articles. Ticket 02's fallback — "no heading of any level →
end of `<main>`" — therefore has a hole: on a non-article page it would happily inject the widget
at the end of `<main>`, because `<main>` is present there too.

Settle:

- **The article test.** Whether `[data-pagefind-body]` is the discriminator, and what the widget
  does when it is absent: render nothing at all, or render at end of `<main>` anyway on the grounds
  that a signup box on the blog index is no bad thing.
- **The two collapsed cases are different.** An article with no headings (the two K8s posts) should
  still get the widget at the end of `<main>`. A page with no `[data-pagefind-body]` may not be an
  article at all. Whether one rule covers both or they are genuinely separate branches.
- **Whether a silent no-op is acceptable**, or whether the widget should say something to a
  developer — a `console.warn`, nothing at all — when it declines to inject. Bearing in mind the
  blog contract is one `<script type="module">` line that someone could paste anywhere.
- **`Games/`.** The directory exists in the deploy target but has no HTML at the paths checked
  above; worth a look before deciding, in case it is a surface the script could reach.

Consult `/grilling`.

## Answer

**The question dissolves, and takes `[data-pagefind-body]` out of the widget with it.** The ticket
assumed the widget must work out at runtime whether it is on an article. It must not: the
`<script type="module">` line goes in `Article.cshtml`, so **BlogToHtml decides article-ness at
generation time** and the widget only ever runs where an article exists. There is no article test,
no discriminator, and no non-article branch.

### One container, one rule

The widget reads `<main>` and heading tags. Nothing else.

- Headings `h2`–`h6` within `<main>`, never `h1` (the `sr-only` article title) — anchor before the
  one whose `offsetTop` is nearest `<main>`'s vertical midpoint, measured after load.
- No `h2`–`h6` at all (the two K8s posts) → append to the end of `<main>`.

**This amends ticket 02's anchor rule**, which measured the midpoint of `[data-pagefind-body]`. That
box was doing exactly one job — being the thing whose midpoint gets measured — and `<main>` does it
equivalently: `Article.cshtml` is two children, `.article-header` (date and tag badges, ~40px) and
then `<div data-pagefind-body>` holding the entire rendered article, so `<main>`'s midpoint sits
about 20px above pagefind-body's. On a 4,000px post that never changes which heading is nearest.
Ticket 02's measurements (median 4.3pp deviation across 27 articles, worst case 83.6% on
`Part6-VirtualMachines`) stand.

The ticket's framing of "two collapsed cases" was wrong: it is one rule with a fallback, because the
case that made them two — a page that is not an article — cannot occur in the widget's code.

### What this retires

`[data-pagefind-body]` leaves the widget's vocabulary. `pagefind.yml` sets no `root_selector`, so
that attribute is the only thing telling Pagefind what to index and it is genuinely load-bearing for
the blog's search — which is precisely why the widget should not be reading it for an unrelated
purpose. A Pagefind config change or a search-related template edit can no longer move the widget or
stop it appearing. `<main>` lives in `_Layout.cshtml` and has been there since the blog's Bootstrap 4
days, so the one remaining hook is also the more stable of the two.

The widget's injected element still *lands* inside that div in the normal case, purely because that
is where the headings are — but nothing queries it. [The widget's accessibility
bar](09-widget-accessibility-bar.md)'s `aside` → `complementary` argument is unaffected: the parent
is a plain `div` in the anchored case and `<main>` itself in the fallback case, and neither is
sectioning content.

### No runtime signal

**No `console.warn`, no guard-with-diagnostics.** A `<main>`-missing page is not a reachable state
for a BlogToHtml-generated page, so there is nothing to warn about. `Games/` closes the same way:
`Games/BuzzerBee/index.html` is Vite-built with its own bundle, carries no `<main>` and no
`six-sided-header`/`footer`, and is not generated by BlogToHtml — the contract line cannot reach it.

### One thing for the spec, not for code

The spec must name the file: the line belongs in **`Article.cshtml`, explicitly not
`_Layout.cshtml`**. All eight of the blog's existing script tags are in the layout (`_Layout.cshtml`
lines 16–18 and 72–76) and `Article.cshtml` has no scripts at all, so the layout is where a
reasonable person would put a ninth — and that would ship the widget to `Blog/index.html` and
`Blog/all.html`, which have `<main>` but are lists of post links. Documenting the file is the cheap
version of the runtime guard deliberately not built.

### Facts established, for the spec

- `_Layout.cshtml:65` owns `<main role="main" class="pb-3">`, shared by the Article, Index and All
  templates — which is why `<main>` is on every page and was never a usable article test.
- `data-pagefind-body` appears in `Article.cshtml:25` alone: 27 of the 29 HTML files under `Blog/`
  carry it, the exceptions being `Blog/index.html` and `Blog/all.html`.
- The CSP in `_Layout.cshtml:8` allows `script-src 'self' https://www.sixsideddice.com`, so a
  `/subscribe/widget.js` module loads on every article.
