# 04 — Vite build shape: three pages plus a one-file widget

Type: research
Status: resolved
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

## Answer

**The two shapes compose in one `vite.config.js`, but never in one bundler pass.** Two passes are
mandatory — the bundler refuses the alternative — but both can live in a single config driven by a
plain `vite build`.

- **Findings**: branch `research/04-vite-build-shape`, commit `436eb80`, at
  `.scratch/email-subscription-frontend/research/04-vite-build-shape.md`. Much of it is
  **empirical**, not just documented — the agent built real bundles and inspected `dist/`.
- **Versions established from source**, not assumed: Vite **8.2.2** (which now bundles
  **Rolldown**, not Rollup — `build.rollupOptions` is a deprecated alias of `build.rolldownOptions`),
  Lit **3.3.3**, Vitest **4.1.11**, happy-dom **20.11.8**. Every finding was reproduced on Vite
  **7.3.6** as well, with identical results.

### The six questions

**1. One config, two passes.** A single pass is impossible, and this was proved rather than
inferred: with four entries Rolldown hoists Lit into a shared chunk (the widget came out at 0.40 kB
importing a 14.98 kB chunk), and the escape hatches are single-entry-only on both Rolldown and
Rollup 4 — `codeSplitting: false` and `inlineDynamicImports: true` are simply refused. The
recommended shape is one `vite.config.js` using the Environment API (`environments: { client, widget }`
plus `builder.buildApp`) run by a plain `vite build`; the fallback is two config files chained as
`vite build && vite build --config vite.widget.config.js`.

**2. `build.lib` alone pins the filename.** In the widget's own pass,
`{ entry, formats: ['es'], fileName: 'widget' }` with `outDir: 'dist'` and `emptyOutDir: false`
emits exactly `dist/widget.js` — unhashed, self-contained — while the pages keep hashing in the
other pass. No `entryFileNames` needed.

**3. `import.meta.env.VITE_API_BASE_URL` is substituted identically in library mode.** Documented
verbatim and confirmed in the bundle: the literal URL appears twice, with no surviving
`import.meta.env`. No workaround needed.

**4. Light DOM costs all component CSS.** `createRenderRoot() { return this; }` opts out, and
`static styles` then **fails silently** — Lit's default `createRenderRoot` is the only caller of
`adoptStyles`, so there is no `<style>`, no adopted sheet, and no warning, yet `elementStyles` is
still populated. `<slot>` stops working too. The recommendation is that the widget owns **no** CSS
at all, using Bootstrap Darkly classes only.

**5. Production Lit is selected by default** under `vite build`, via Vite's default
`resolve.conditions`. Proved by forcing the other branch: 19.16 kB → 33.86 kB raw. A measured
`widget.js` carrying Lit plus two light-DOM components is **19.7 kB raw / 6.6 kB gzip / 5.9 kB
brotli**; the finished widget is estimated at **7–8 kB gzip**. Lit's own published "around 5 KB"
matches the brotli figure, not the gzip one.

**6. `environment: 'happy-dom'` plus the package is the whole setup** for Vitest. Define/get,
upgrade from `innerHTML`, light-DOM render, `updateComplete` and reactive re-render were all
verified working.

### Three traps that must reach the spec

- **`"type": "module"` in `package.json` is load-bearing.** Without it, lib mode emits `widget.mjs`
  and the blog's hard-coded `https://www.sixsideddice.com/subscribe/widget.js` 404s. A one-word
  omission silently breaks the widget on every article.
- **Never use a `--mode widget` switch** for the second pass. It silently drops `.env.production`
  and compiled the API URL to `void 0` — a widget that builds clean and talks to nowhere. The
  suggested guard is a CI grep of `dist/widget.js` for the expected origin, which belongs to
  [Frontend CI and the build-env channel](07-frontend-ci-and-build-env.md).
- **Never `import './widget.css'`.** Lib mode emits it as a separate `dist/widget.css` that a bare
  `<script>` in someone else's page will never fetch.

### Two constraints later tickets inherit

- **The form cannot be a form-associated custom element.** happy-dom has no
  `ElementInternals`/`attachInternals` at all, so the shared form component in
  [Prototype the three subscription pages](06-prototype-the-subscription-pages.md) must not use
  that API if it is to stay testable. Its other gaps are minor:
  `customElements.upgrade()` is a literal no-op ("Not implemented yet" in source) and
  `whenDefined()` resolves `undefined` rather than the constructor.
- **Vitest runs the *dev* build of Lit** (`NODE_ENV=test`), so "Lit is in dev mode" in test output
  is correct and not a misconfiguration worth chasing.

### The undocumented bit

The custom build environment needs `consumer: 'client'`. Without it the widget built to 1.08 kB with
`import { LitElement } from "lit"` left external and unminified — a bundle that would fail in the
blog's page. This appears on no docs page the agent could find; it is empirical only, which is
itself a reason to weigh the fallback shape.

### Not established

Whether the Environment API survives to Vite 9 unchanged — Vite calls it "release candidate phase"
with possible breaking changes, which is the fork
[Choosing the Vite build shape to commit to](12-vite-build-shape-decision.md) now has to settle.
Also open: whether `build.lib` alongside a multi-HTML `input` is supported or merely tolerated
(undefined behaviour — `input` won and `fileName` misbehaved, producing `widget4.js`); where
light-DOM component CSS *should* go, on which Lit says only "you must decide"; whether happy-dom's
`:defined` selector matches correctly; and whether GitHub Pages serves brotli, which decides whether
the 5.9 kB or the 6.6 kB figure is the real one.
