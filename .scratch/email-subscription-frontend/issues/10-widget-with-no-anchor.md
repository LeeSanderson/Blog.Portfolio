# 10 — What the widget does when there is no anchor

Type: grilling
Status: open
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
