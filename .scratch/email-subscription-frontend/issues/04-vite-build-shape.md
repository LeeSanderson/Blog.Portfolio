# 04 — Vite build shape: three pages plus a one-file widget

Type: research
Status: open
Blocked by: —

## Question

One Vite project has to emit two shapes at once, and it is not obvious they compose:

- Three HTML pages (`/subscribe/`, `/subscribe/confirm/`, `/subscribe/unsubscribe/`) — a
  multi-page build, hashed assets, normal `<script type="module">` output.
- **One** self-contained `widget.js` at a **stable, unhashed** path, because a third repo's
  post template hard-codes `https://www.sixsideddice.com/subscribe/widget.js`. It must inline
  Lit and everything else — a bare `<script>` in someone else's page cannot chase a chunk graph.

Establish against the Vite and Lit documentation:

- Whether MPA mode and a single-file library build can live in one `vite.config.js`, or whether
  this needs two build passes wired into one `npm run build`.
- How to force one entry to a fixed filename with no hash and no code-splitting, while the pages
  keep hashing.
- Whether `import.meta.env.VITE_API_BASE_URL` is substituted in a library-mode build the same way
  it is in an app build — if not, how the base URL reaches the widget.
- How Lit renders into light DOM (`createRenderRoot()`), and what that costs: `static styles`
  stops working, so confirm where component CSS goes when there is no shadow root.
- Whether Lit's dev-mode warning bundle is excluded from the production build, and what the
  resulting `widget.js` weighs — the widget loads on every article, so the number matters.
- What Vitest + happy-dom needs to register custom elements, given happy-dom's `customElements`
  support and Lit's reliance on it.

Prefer primary sources: the Vite and Lit docs, not blog posts. Capture findings with `/research`.
