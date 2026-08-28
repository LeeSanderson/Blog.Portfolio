# 12 — Choosing the Vite build shape to commit to

Type: grilling
Status: resolved
Blocked by: 04

## Question

Surfaced by [Vite build shape: three pages plus a one-file widget](04-vite-build-shape.md), which
established that the two-pass build is unavoidable and then found *two* ways to express it. The
research can say what works; it cannot say which risk this repo should carry.

**Option A — one config, Environment API.** `environments: { client, widget }` plus
`builder.buildApp`, run by a plain `vite build`. One file, one command. But Vite itself calls the
Environment API "release candidate phase" with possible breaking changes, and the research found the
shape depends on `consumer: 'client'`, a requirement that appears on no documentation page and is
known only because a build was run and inspected. An undocumented flag in an RC API is the part that
breaks quietly on upgrade.

**Option B — two config files chained.** `vite build && vite build --config vite.widget.config.js`.
Duller, one more file, and the shared settings have to live somewhere both can read. In exchange it
uses only long-stable API and was verified identically on Vite 7.3.6 and 8.2.2, so the Vite major
stops being a coupled decision.

The two questions are one decision, because A forces Vite 8 while B runs on either:

- Which shape does the spec tell the implementing agent to build?
- Which Vite major gets pinned, and does that pin belong in an ADR? The map's standing preference is
  to offer an ADR for any new library or tool, and this effort is already adding Lit, Vite and
  Vitest — so the question may be less "does Vite need an ADR" than "does the build shape need to be
  *in* it".

Worth weighing while deciding: this widget loads on every article, so a build that breaks silently on
a dependency upgrade fails in public with no signal in this repo — the same shape of risk the map
already named for light-DOM styling and for the anchor rule's dependency on BlogToHtml's markup. Also
note the research's `--mode widget` trap: the failure mode for a mis-wired second pass is a widget
that builds clean and talks to nowhere, which argues for whichever shape is easiest to guard in CI.

This is a decision, not more reading. The facts are all in
`.scratch/email-subscription-frontend/research/04-vite-build-shape.md` on branch
`research/04-vite-build-shape`.

**Amended by [Frontend CI and the build-env channel](07-frontend-ci-and-build-env.md).** This ticket's
last paragraph argued for "whichever shape is easiest to guard in CI" — that guard is now settled, and
it changes the calculus rather than just informing it. A four-assertion `postbuild` script
(`scripts/verify-dist.mjs`) runs on every `npm run build`, in CI *and* on a developer's laptop, and it
is **shape-independent**: it asserts on `dist/`, not on how `dist/` was produced. One of its four
assertions — no surviving bare-specifier imports (`from "lit"`) — is precisely the failure mode of
Option A's undocumented `consumer: 'client'` requirement, which built a 1.08 kB widget with Lit left
external. So **Option A's quiet-breakage-on-upgrade risk is no longer quiet**: it fails the build
loudly, at the moment of the upgrade, in this repo. That was the main argument for the duller Option B,
and it is substantially weaker now. The Vite-major half of the decision is untouched.

## Answer

**Option A, on Vite 8.2.2, with exact pins, one ADR, and one extra config line.** The two halves of
the question turned out not to be one decision at all, and separating them flipped the second one's
sign.

### The premise this ticket was written on is false

> The two questions are one decision, because A forces Vite 8 while B runs on either.

It does not. The research measured Shape A on **both Vite 7.3.6 and 8.2.2** — labelled
`[measured, both Vite 7.3.6 and 8.2.2]`, with `consumer: 'client'` recorded as "stable and reproducible
on both Vite 7 and 8". The Environment API is Vite 6+; `rolldownOptions` in the research's Shape A
snippet is incidental, and reads `rollupOptions` on Vite 7. Shape and major are independent, and the
coupling was the only thing making the Vite major look like a *cost* of Option A.

Uncoupled, the RC dependency argues the other way: **if the build leans on an API in release-candidate
phase, the newest major is the right place to stand.** The Environment API is converging toward stable,
so pinning Vite 7 buys an older snapshot of the same RC surface *plus* a superseded major line — the
worst of both. ADR-0012 already ruled on that shape of question in this repo, choosing AwesomeAssertions
9.x over a zero-source-change 8.x swap because "it is a superseded major line, which is exactly the trap
ADR-0010 documents".

### The five decisions

| | Decision |
|---|---|
| Build shape | **Shape A** — one `vite.config.js` carrying `environments: { client, widget }` and a `builder.buildApp` hook; `npm run build` is a plain `vite build` |
| Vite major | **8.2.2** — `latest`, Rolldown, `build.rolldownOptions` |
| Pin style | **Exact**, no carets: `vite 8.2.2`, `lit 3.3.3`, `vitest 4.1.11`, `happy-dom 20.11.8` |
| Widget pass | additionally sets `rolldownOptions: { output: { codeSplitting: false } }` |
| ADR | **One**, covering all of the above |

### Why Shape A survives its own RC dependency

The amendment said the guard makes Option A's risk "no longer quiet". It is stronger than that, and the
reason is structural rather than lucky. Each plausible Environment-API regression was walked through to
see what it actually *looks like on disk*, and every one lands in `dist/`, which is the only thing
`verify-dist.mjs` inspects:

| Regression | What `dist/` looks like | Assertion that fires |
|---|---|---|
| `builder.buildApp` no longer honoured, or `environments` renamed | second pass never runs; no `dist/widget.js` at all | 1 — `dist/widget.js` exists |
| `consumer: 'client'` renamed or its default changes | 1.08 kB `widget.js` starting `import { LitElement } from "lit"` | 4 — no surviving bare specifiers |

That is not a coincidence of which four assertions ticket 07 happened to pick. The Environment API's
entire job in this config is *to produce the second file*, so its failure domain and the guard's
inspection domain are the same set. The guard runs as an npm lifecycle hook on every `npm run build`, on
a laptop as well as in CI, so the regression surfaces at the moment of the upgrade, in this repo, to
whoever ran the upgrade.

Its blind spot is `vite dev`, which produces no `dist/` — see the residue below.

### The field, including two shapes the ticket did not list

- **Shape B′** — B with `vite.widget.config.js` doing `mergeConfig(base, …)` against `vite.config.js`.
  Named because it dissolves B's only real cost, "the shared settings have to live somewhere both can
  read", using long-stable documented API. This is the **named fallback** if the dev-server residue
  below turns out badly, and it is why that residue is a cost rather than a risk: the escape hatch is
  already fully specified.
- **Shape D** — one config, second pass keyed off `process.env.WIDGET` (the research's own "if a
  single-config switch is wanted, key it off `process.env.<SOMETHING>` — never off `mode`").
  **Rejected**: setting an env var inline in an npm script is not cross-platform, so on Windows it needs
  `cross-env`. A dependency added to save one file, when `--config` is free.

Shape A's win over B′ is small and honest: one file and one command against two of each. The decision
rests on the guard removing B′'s safety advantage, not on A being much nicer.

### Exact pins, because this is the repo's first `package.json`

There is no npm anywhere in this repo today — no `package.json`, no `.nvmrc` — so whatever this file
does becomes the monorepo's npm convention. The two available precedents disagree: this repo pins every
NuGet package exactly through `Directory.Packages.props`, with prose comments on the two deliberate
holds; BuzzerBee, the Vite app in the neighbouring repo that deploys to the same GitHub Pages site, uses
`^` throughout.

`npm ci` reads the lockfile either way, so this changes nothing about what CI installs. It changes how a
version move becomes **visible**: exact pins make a Vite bump a one-line `package.json` diff someone
reads, where carets make it a `package-lock.json` diff nobody does. That matters more here than it
usually would, because Shape A rests on behaviour documented nowhere — a Vite version move is precisely
the moment `verify-dist.mjs` is being trusted to catch something no docs page warned about, and it should
be a moment a human notices.

### One ADR, with the shape inside it

Everything above shares a single revisit trigger: **the next Vite major tests the Environment API pin,
the `consumer: 'client'` flag, the exact pins and Vitest's compatibility range simultaneously.** By this
repo's own cutting rule they are therefore one ADR, not four — ADR-0011 and ADR-0012 were "recorded
separately, because the two decisions have different drivers and will be revisited independently", and
these do not have different drivers. ADR-0012 also settles the narrower question the ticket asked, by
precedent: it carries its major-line choice *inline* rather than in a separate document.

Shape A belongs in it, and not because reversing it is expensive — reversing it is cheap. It is that the
reasoning is unrecoverable from the code. `consumer: 'client'` sitting in `vite.config.js` reads as
boilerplate, and nothing in the repo would tell the next person that deleting it emits a 1.08 kB widget
with Lit left external, or that the fourth assertion in `verify-dist.mjs` exists to catch exactly that
one line. An ADR that recorded "we use Vite 8" and not that pairing would leave the guard's most
important assertion with no recorded reason.

Its working title: **npm and Vite enter the monorepo** — the pins, Shape A, the RC-API and
`consumer: 'client'` exposure, `verify-dist.mjs` as the mitigation, and Vitest + happy-dom.
Lit-in-light-DOM is deliberately **not** in it: Lit is swappable without touching the build and the build
is bumpable without touching Lit, which is the different-drivers test. Where that ADR and an
`Aspire.Hosting.JavaScript` one land is [Write the spec and implementation
tickets](08-write-the-spec-and-tickets.md)'s to place.

### The dynamic-import hole, found by putting ticket 04 beside ticket 07

Neither ticket could see this alone, and it is the map's signature failure shape.

Ticket 04 measured that library mode does **not** inline dynamic imports by default on Vite 8: one
`await import('./lazy-part.js')` in the widget entry emitted `lazy-part-C_1DLs33.js` (0.09 kB) alongside
`widget.js`. It handled that with a spec rule — "no dynamic `import()` inside the widget entry, and this
option is never needed" — written before ticket 07 designed the guard.

**None of the four assertions catches it.** The emitted import is `import("./lazy-part-C_1DLs33.js")`,
which is *relative*, so the no-bare-specifiers check passes; `dist/widget.js` still exists, still carries
the origin, still emits no CSS. The extra chunk simply sits in `dist/`, gets published, and a bare
`<script>` on a blog article 404s chasing it. Builds clean, guard green, broken in every reader's
browser with nothing red in this repo — and unlike BlogToHtml markup drift, this one originates *here*,
triggered by someone doing the reasonable thing and lazy-loading to trim the bundle.

Closed by adding `rolldownOptions: { output: { codeSplitting: false } }` to the widget environment.
Ticket 04 measured the effect: the split collapses back to a single 19.52 kB `widget.js`. The pass is
single-entry, so Rolldown accepts the option (it is refused only with multiple inputs — the same
constraint that forced two passes in the first place). One line, no tuning, nothing to maintain, and it
makes the failure **impossible rather than detected**, which beats a fifth assertion. A fifth assertion
was the alternative and was declined for that reason. The line goes in the ADR beside
`consumer: 'client'`, for the same reason: it too reads as boilerplate.

### Residues for the spec, not decisions

- **`vite dev` is unverified under Shape A.** The research measured builds only, and Aspire drives the
  dev server (ticket 05's `AddViteApp`). A build-only `widget` environment should sit inert in dev —
  `builder.buildApp` is a build hook and `build.lib` is ignored by the dev server — but "should" on an RC
  API is the exact discomfort this ticket exists to price, and it is the one place `verify-dist.mjs`
  cannot reach, because `npm run dev` produces no `dist/`. It is a **cost, not a risk**: it fails loudly,
  locally, and immediately for whoever runs `./run-local.ps1`. The spec instructs the implementing agent
  to verify that `./run-local.ps1` serves all three pages with the `widget` environment declared, and
  names **Shape B′** as the fallback if it does not.
- **`build.target`** defaults to `'baseline-widely-available'` on Vite 8. Reasonable for a blog audience,
  but the widget runs in arbitrary readers' browsers, so the spec sets it explicitly rather than
  inheriting it, so that it reads as a choice.
- **Node.** Vite 8.2.2 declares `engines.node: "^20.19.0 || >=22.12.0"`. Both feet clear it —
  `node-version: "22"` in ticket 07's workflow, and 22.20.0 on the dev machine — but the spec states the
  floor, since this is the repo's first Node-version dependency.

### Facts looked up rather than assumed

- `vite@8.0.0` published **2026-03-12**, so Vite 8 was 5½ months old at the time of this decision, not
  fresh paint. `8.2.2` published 2026-08-20 and is `latest`; `previous` is `7.3.6` (2026-06-25), so the 7
  line is still patched but superseded.
- `vite@8.2.2` `engines.node`: `^20.19.0 || >=22.12.0`.
- No `package.json`, `.nvmrc` or `.node-version` exists anywhere in this repo.
- `C:/Dev/Personal/BuzzerBee` runs `vite ^6.2.0` with caret ranges throughout.
- Local toolchain: Node 22.20.0, npm 10.9.3.
