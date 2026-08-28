# 14 — The drift signal for the vendored blog CSS

Type: grilling
Status: open
Blocked by: —

## Question

Graduated from the map's fog ("Keeping the vendored Bootstrap Darkly copy honest"), which has now been
narrowed twice and is finally sharp.

[Prototype the three subscription pages](06-prototype-the-subscription-pages.md) removed half of it: the
header and footer are **not** vendored — they load live from `www.sixsideddice.com/js/` by
fully-qualified URL, so they cannot drift. What remains is the CSS alone: the three pages vendor
`bootstrapdarkly.min.css` and `site.css`, copied out of `C:/Dev/Personal/leesanderson.github.io`.

[Frontend CI and the build-env channel](07-frontend-ci-and-build-env.md) removed the other half of the
vagueness: `scripts/verify-dist.mjs` now exists, running on every `npm run build` in CI and locally, so
there is an obvious home for a check. *Where* it would run is no longer the question.

Settle:

- **Whether anything checks the vendored copies at all**, or whether drift is simply accepted and
  documented. The pages are chrome-only; the plausible consequence of drift is cosmetic, not broken —
  which is a real argument for accepting it, unlike the widget's failure modes.
- **What the signal is, if there is one.** A committed checksum the guard script compares against; a
  build-time fetch of the live blog's CSS and a diff; a scheduled workflow rather than a build-time
  check; or a documented refresh step in the app's README with no automation.
- **What a build-time fetch would cost.** It makes `npm run build` depend on `www.sixsideddice.com`
  being up, which would fail deploys for a reason unrelated to the change being deployed — and this
  effort has already rejected that shape of coupling once, when it declined a `public/` copy of the
  header in favour of a live URL.
- **Whether the widget is affected.** It should not be — [Vite build shape](04-vite-build-shape.md)
  established the widget owns **no** CSS at all, using Bootstrap Darkly classes inherited from the
  article page. Confirm that means the widget has no vendored asset to drift, so this ticket is about
  the three standalone pages only.

Note the asymmetry worth weighing: a widget that silently breaks fails in public and loses
subscriptions; vendored CSS that silently drifts makes three low-traffic pages look slightly wrong. The
map's habit of guarding silent failures should not be applied reflexively where the stake is this much
lower.
