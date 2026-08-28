# 12 — Choosing the Vite build shape to commit to

Type: grilling
Status: open
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
