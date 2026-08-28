# Email Subscription Frontend

Wayfinder map. Tickets are the files in `issues/`.

## Destination

A spec at `.scratch/email-subscription-frontend/spec.md` plus its numbered implementation
tickets, ready to hand to an implementing agent — describing a Lit + Vite frontend at
`apps/email-subscription/frontend/` that ships a sign-up page, confirm and unsubscribe
landing pages, and a self-injecting article widget. **This map writes no production code**;
the only code it produces is throwaway prototypes.

## Notes

**Domain.** This monorepo, plus two read-only neighbours that are cloned locally and should
be read rather than guessed at:

- `C:/Dev/Personal/leesanderson.github.io` — the deploy target. Static site, `.nojekyll`,
  Bootstrap Darkly (`css/bootstrapdarkly.min.css`) + `css/site.css`, Font Awesome, and two
  existing light-DOM custom elements (`six-sided-header`, `six-sided-footer` in `js/`).
- `C:/Dev/Personal/Blog` — `BlogToHtml`, the .NET generator that emits the articles. Its
  post template carries a CSP meta allowing `script-src 'self' https://www.sixsideddice.com`
  and sets no `default-src`/`connect-src`, so a `/subscribe/` script loads and `fetch()` to
  the Function App is unrestricted.

**Skills every session should consult.** `/grilling` and `/domain-modeling` for decisions,
`/prototype` for the two prototype tickets, `/research` for the research tickets. Read
`CODING_STANDARDS.md`, `CONTEXT.md` and `docs/adr/` before proposing anything an ADR already
settled.

**Standing preferences.** Leanest set of distinct behaviours in tests, not coverage theatre.
Offer an ADR for any new library or tool, even a cheaply reversible one — this effort adds
Lit, Vite, Vitest and `Aspire.Hosting.JavaScript`, so ADR authoring is in the destination's
scope. Avoid code comments; let names carry intent.

**Settled at charting** (from the `/grilling` session that produced this map — these are
constraints on every ticket, not open questions):

| | Decision |
|---|---|
| Seam | This repo owns whole pages *and* the widget bundle, deployed to `/subscribe/` on `leesanderson.github.io` via the existing `_reusable-frontend-deploy.yml` (ADR-0002). *That reusable workflow is **deleted**, along with `.github/workflows/templates/`: the seam is re-cut as two composite actions (`publish-to-pages`, `smoke-test-api`) called from a per-app one-job workflow, so each app owns its own install/build/test — see [Frontend CI and the build-env channel](issues/07-frontend-ci-and-build-env.md)* |
| Stack | Lit + Vite + Vitest/happy-dom; npm enters the monorepo as dev tooling |
| Widget state | Four states in `localStorage`. Opt-out is permanent; dismissal ages out. *The placeholder names chosen here were replaced by `submitted`/`confirmed`/`optedOut`/`dismissed` — see [Name the reader's local subscription state](issues/01-name-the-readers-local-state.md)* |
| Surfaces | `/subscribe/`, `/subscribe/confirm/`, `/subscribe/unsubscribe/`, `/subscribe/widget.js`; one shared form component. *That component mounts on **two** surfaces, not four: the widget and `/subscribe/`. Neither landing page carries the form in any state, and `/subscribe/` never suppresses itself the way the widget does — see [Prototype the three subscription pages](issues/06-prototype-the-subscription-pages.md)* |
| Local dev | An Aspire resource so `./run-local.ps1` runs the whole stack; `npm run dev` still works standalone. *The call is `AddViteApp` from `Aspire.Hosting.JavaScript` — `AddNpmApp`/`Aspire.Hosting.NodeJs` named here at charting do not exist at 13.4.6. And the resource does **not** fail to start in CI: Aspire 13 installs the dependencies instead, which is a quieter problem, not accepted noise — see [Aspire hosting for the Vite dev server](issues/05-aspire-vite-dev-server.md)* |
| Local email | Real Resend sends, gated on verifying the sending domain |
| API URL | Repo variable `API_BASE_URL` + a new generic `build-env` input on the reusable workflow; Vite bakes `VITE_API_BASE_URL`. *The `build-env` input was **dissolved, not designed**: with the app owning its own build there is no generic channel — the build step just sets `env: VITE_API_BASE_URL: ${{ vars.API_BASE_URL }}`. The variable holds the **full origin**, not the bare hostname — see [Frontend CI and the build-env channel](issues/07-frontend-ci-and-build-env.md)* |
| Testing | Vitest + happy-dom, no browsers. *The scroll-trigger seam this row called for is moot — there is no scroll trigger; see [Prototype the article widget](issues/02-prototype-the-article-widget.md). The layout dependency moved to the anchor rule instead, and where the seam goes now is [Testing the anchor rule without a layout engine](issues/11-testing-the-anchor-rule.md)* |
| Styling | Light-DOM Lit following the `six-sided-*` precedent, inheriting Bootstrap Darkly; `bootstrapdarkly.min.css` + `site.css` vendored for the pages and local dev |
| Blog contract | One `<script type="module">` line in the BlogToHtml post template; the widget self-injects |

Edges named rather than buried — a running list, added to as tickets resolve. They share one shape:
**the widget fails in public with no signal in this repo**, which is why the map keeps collecting them.

- ~~Choosing real Resend sends makes the whole local confirm/unsubscribe loop depend on
  [Verify the sixsideddice.com sending domain in Resend](issues/03-verify-resend-sending-domain.md).~~
  **Closed:** the domain is verified and local dev is wired to it, so real sends work and the
  file-drop `IEmailSender` fallback is not needed.
- Light DOM buys a native look at the price of coupling: a Bootstrap upgrade or a `site.css`
  edit on the blog can restyle or break the widget with no signal in this repo, and no test
  here will catch it. **Sharpened by
  [The widget's accessibility bar](issues/09-widget-accessibility-bar.md): the coupling is no longer
  only cosmetic.** The widget ships no CSS, so two of its accessibility decisions now rest on classes
  the blog owns — `.sr-only` hides the heading in the collapsed state (drop it and the collapse
  budget doubles from one line to two), and `.btn.disabled` is the entire visual of the in-flight
  state. Same shape as ever: silently wrong in the reader's browser, no signal here. The honeypot is
  the one place the dependency was deliberately refused, by hiding with the `hidden` attribute rather
  than a class — because there the failure is not cosmetic but a reader told they subscribed when
  they did not.
- The same shape, in markup rather than styling, named while resolving
  [Prototype the article widget](issues/02-prototype-the-article-widget.md): the widget's anchor rule
  reads BlogToHtml's `data-pagefind-body` wrapper and the article's heading markup to place itself.
  A template change in `C:/Dev/Personal/Blog` can silently move the widget, or stop it appearing at
  all, with no signal here and no test that would catch it.
- The same shape again in the *build*, from
  [Vite build shape](issues/04-vite-build-shape.md): three ways to ship a widget that builds clean and
  is broken in the reader's browser — a missing `"type": "module"` renames it to `widget.mjs` and the
  blog's hard-coded URL 404s; a `--mode` switch on the second pass compiles the API URL to `void 0`;
  an imported stylesheet becomes a `dist/widget.css` a bare `<script>` never fetches. None raises a
  warning. The only proposed guard is a CI assertion on `dist/widget.js`, now part of
  [Frontend CI and the build-env channel](issues/07-frontend-ci-and-build-env.md).
- A **fourth** in the build, and the first one that breaks the shape, from
  [Prototype the three subscription pages](issues/06-prototype-the-subscription-pages.md): the pages
  deploy into a subdirectory, so `base` must be `/subscribe/`, and Vite then prefixes `base` onto
  every root-absolute reference it can see — so `<script src="/js/header.js">` silently becomes
  `/subscribe/js/header.js` and can never reach the blog's live header. Named here because it is the
  one case that **fails the build loudly** when there is no copy to find (`Failed to resolve
  /js/header.js`), which is why the chosen mechanism is a fully-qualified URL: it cannot ship broken.
- **Rotating `SigningKey` invalidates every confirm and unsubscribe link ever sent, all at once.**
  Surfaced while resolving
  [Prototype the three subscription pages](issues/06-prototype-the-subscription-pages.md). The links
  never expire by design (ADR-0007), so the signing key is the only thing that can invalidate them,
  and it invalidates all of them together. A human escape hatch was weighed — making the canonical
  from-address a monitored mailbox — and **declined**: the from-addresses are `noreply@` and
  `updates@` and the blog carries no contact address, so the failure page offers retry guidance only.
  Accepted risk: someone whose unsubscribe link is dead has no route out but marking the mail as spam.
- The **second** one to break the shape, from
  [Frontend CI and the build-env channel](issues/07-frontend-ci-and-build-env.md): the deploy's push to
  `leesanderson.github.io` can fail non-fast-forward if a human pushes to the blog between our checkout
  and our push — the `pages-deploy` concurrency group only serialises deploys from *this* repo. It fails
  **loudly** and leaves nothing half-broken, since the push either happened or it did not, which is why
  the chosen answer is to fail rather than rebase-and-retry, with `workflow_dispatch` as the retry.

## Decisions so far

<!-- one line per closed ticket: gist + link -->

- [Name the reader's local subscription state](issues/01-name-the-readers-local-state.md) — the
  browser-local concept is a **Signup Record** (now in `CONTEXT.md`), stated as
  `submitted`/`confirmed`/`optedOut`/`dismissed` so it shares no word with the Subscriber; stored at
  `six-sided.signup` as `{v, state, at}` with no email, anything unrecognised treated as no record;
  `dismissed` ages out at 30 days and `submitted` at 7, `confirmed`/`optedOut` never, as build-time
  constants; both edge cases accepted, with an "I'm already subscribed" control mitigating
  per-browser state.
- [Prototype the article widget](issues/02-prototype-the-article-widget.md) — a **mid-article
  interstitial** wins, anchored before the `h2`–`h6` nearest the article's vertical midpoint (never
  `h1`; end of `<main>` when an article has no headings, as two K8s posts do). **No scroll trigger** —
  it is inserted at load and the reader scrolls into it, which deleted the trigger question and left
  the seam as `promptDecision(record)`; "Not now" and an always-visible "I'm already subscribed"
  collapse the block in place over 200ms, the only motion in the widget; copy is the widget's own and
  never echoes the API's deliberately-hedged `message`.
- [Verify the sixsideddice.com sending domain in Resend](issues/03-verify-resend-sending-domain.md) —
  **done**: verified via 123-reg DNS, test mail reached the inbox, and local dev already carries a real
  key and an on-domain from-address, so `./run-local.ps1` exercises the real confirm/unsubscribe loop
  and the file-drop `IEmailSender` fallback is dead. Free tier caps sending at 100/day, 3,000/month.
  One loose end for the spec, not a decision: local sends from `noreply@` while the azd default is
  `updates@`, so the spec should name the canonical address.
- [Vite build shape: three pages plus a one-file widget](issues/04-vite-build-shape.md) — the two
  shapes compose in **one config but never one bundler pass**; a single pass was *proved* impossible
  (Rolldown hoists Lit into a shared chunk and refuses `codeSplitting: false` with four entries).
  `build.lib` alone pins `dist/widget.js` unhashed while the pages keep hashing, and
  `import.meta.env` substitutes identically in lib mode. Light DOM kills `static styles` **silently**,
  so the widget owns no CSS at all. Measured at **6.6 kB gzip**, est. 7–8 kB finished. Which of the
  two build shapes to commit to is now
  [Choosing the Vite build shape to commit to](issues/12-vite-build-shape-decision.md).
- [Aspire hosting for the Vite dev server](issues/05-aspire-vite-dev-server.md) — the package and
  method named at charting **do not exist** at 13.4.6: it is `AddViteApp` from
  `Aspire.Hosting.JavaScript`, not `AddNpmApp` from the renamed, dead `Aspire.Hosting.NodeJs`. The
  new package still has to be added to `Directory.Packages.props`, at 13.4.6 to match the two Aspire
  packages already pinned there. The charting assumption that the resource fails to start in CI is wrong —
  **Aspire 13 auto-installs the dependencies instead**, so nothing goes red but a backend-only PR
  would silently run a network `npm install`; that decision is now on
  [Frontend CI and the build-env channel](issues/07-frontend-ci-and-build-env.md), which also loses
  its CORS bullet (port 4000 is already allow-listed, and Aspire's `--port` beats `vite.config.js`).
  The API URL reaches the dev server only via an explicit
  `WithEnvironment("VITE_API_BASE_URL", host.GetEndpoint("http"))` — service-discovery vars are
  unprefixed and never reach client code. `WithExplicitStart()` was checked and does not help.
- [Prototype the three subscription pages](issues/06-prototype-the-subscription-pages.md) — the pages
  wear the blog's **full chrome** (variant B, `search.html`'s shell), loading `six-sided-header`/
  `footer` by **fully-qualified URL**: `base` must be `/subscribe/` and Vite rewrites every
  root-absolute reference, so a same-origin tag can never reach the blog's live header, and a
  `public/` copy is ship-copies in disguise. **Both landing pages require a click** rather than firing
  on load — the only configuration with no silently-wrong outcome, and the one that keeps ADR-0007's
  stated `GET` safety property true rather than merely assumed. The pages **write the Signup Record**
  (confirm → `confirmed`, unsubscribe → `optedOut`), which gives `optedOut` its only writer and stops
  the widget telling a reader who just confirmed that it is still waiting. `?id=&sig=` is **left in
  the address bar** — the token is permanent, so stripping revokes nothing and a refresh before
  clicking would strand the reader. Full copy for all 15 states is on the ticket; `offline` is kept
  distinct from `failure` throughout, and **no page ever says a link expired**, because links never do.
- [Frontend CI and the build-env channel](issues/07-frontend-ci-and-build-env.md) — the `build-env`
  question was **dissolved, not answered**: readability drove the seam to be re-cut so each app owns its
  own install/build/test, and then there is no generic channel to design. `_reusable-frontend-deploy.yml`
  (zero real callers) **and** `.github/workflows/templates/` are deleted, replaced by two composite
  actions — `publish-to-pages` and `smoke-test-api` — called from a **single linear job**, because a
  composite runs in the calling job and any two-job shape rebuilds, meaning *the bytes that ship were
  never the bytes tested*. `API_BASE_URL` is a repo **variable** holding the **full origin** (public by
  construction; masking would destroy the only cheap diagnostic). The staleness premise was **wrong** —
  `uniqueString` is deterministic, so recreating the azd env under the same name/subscription/location
  keeps the same hostname — leaving two distinct failures with one check each: the frontend's pre-publish
  ping (typo, dead app) and a `backend-cd.yml` hostname comparison (env genuinely renamed). The widget
  guard is a **four-assertion `postbuild` script**, not a CI step, covering all three of ticket 04's traps
  plus its undocumented `consumer: 'client'` one, and doubling as the loud failure for an unset variable.
  Aspire's npm-install leak is cut by **removing the `frontend` resource in the E2E test** (`Resources`
  verified to exist at 13.4.6) — a backend ping test has no dependency on a Vite dev server, and Aspire
  runs `npm install`, not `ci`, so the leak could mutate the lockfile invisibly in CI. Action-version
  sweep across the whole repo is **one line**: `backend-cd.yml`'s `checkout@v4` → `@v7`.
- [The widget's accessibility bar](issues/09-widget-accessibility-bar.md) — the widget is an
  **`<aside>` named by its own heading**, giving one keystroke past the promo on every post, with a
  **fixed `h2`**: the anchor rule was measured across all 27 articles (`h2` on 16, `h3` on 8, `h4` on 1,
  no heading on 2), so a fixed level skips on **9 of 25** — accepted, because skipping is axe
  best-practice and not a WCAG failure, and the `aside` bounds the false nesting. **One always-present
  polite `role="status"`** carries `Sending…`, network errors and invalid email; nothing is assertive,
  because every message answers a button just pressed. `pendingNote` is *not* a live message — it
  renders at load. **One focus rule covers all three destroying actions** (success, "Not now", "I'm
  already subscribed"): a `tabindex="-1"` replacement focused with `preventScroll`, announced once by
  the focus move and never also a live region — and moved **on click, not `transitionend`**, which under
  `prefers-reduced-motion` never fires. Both prototypes' `button.disabled` is replaced by
  **`aria-disabled` + a handler guard**, because disabling the focused element drops focus to `<body>`
  and the *error* path never repairs it. The honeypot becomes **`hidden`**, not off-screen — the widget
  ships no CSS, so hiding must travel with the markup. Two questions the ticket never listed were
  surfaced and settled: **ids are namespaced `six-sided-signup-`** (BlogToHtml slugifies headings into
  the same id space, and a collision silently unlabels the input), and invalid input uses
  **`novalidate` through the same status region**, adding one row to ticket 02's copy table.
  `prefers-reduced-motion` confirmed as one branch.

## Not yet specified

- **Analytics.** Whether subscribe/confirm/dismiss feed the blog's existing Google Analytics
  tag, and whether that is wanted at all.

## Out of scope

- **The BlogToHtml template edit.** The spec documents the exact `<script>` line; adding it
  is separate work in `C:/Dev/Personal/Blog`, not on this map.
- **A custom `api.sixsideddice.com` domain.** Weighed at charting and rejected in favour of
  the repo variable — revisit only as its own effort, with the DNS and certificate work owned.
- **Playwright, browser-mode or end-to-end tests.** Deliberately traded away; the scroll
  trigger is made testable by a seam instead.
- **A server-side "am I subscribed?" endpoint.** It would be an email-enumeration oracle and
  would undo the anti-enumeration design the backend spec chose on purpose.
- **A re-subscribe affordance on the unsubscribe page.** Re-asks someone who just opted out.
- **Any change to the backend's subscribe/confirm/unsubscribe contract.** The frontend is
  built against it as it stands.
- **Digest headroom against the Resend free tier's 100/day cap.** Surfaced while resolving
  [Verify the sixsideddice.com sending domain in Resend](issues/03-verify-resend-sending-domain.md):
  the weekly digest sends one message per Active Subscriber on one day, so it starts failing at
  roughly 100 subscribers. Real, but it is the backend digest's problem — this frontend sends nothing
  and its signup confirmations sit orders of magnitude below the cap. Belongs to a future backend
  effort, not this map.
- **The apex domain's broken HTTPS.** Surfaced while resolving
  [Name the reader's local subscription state](issues/01-name-the-readers-local-state.md):
  `http://sixsideddice.com` 301s to `www`, but `https://sixsideddice.com` fails TLS, and the backend's
  CORS allowlist includes that effectively-dead origin. Blog infrastructure, not this effort.
- **Dependabot for GitHub Actions (and for npm/NuGet).** Offered while resolving
  [Frontend CI and the build-env channel](issues/07-frontend-ci-and-build-env.md) as the durable answer
  to keeping action pins current, and **declined** — the one-line sweep stands. Widening it to the npm
  and NuGet ecosystems is a bigger call anyway, since NuGet is centrally pinned via
  `Directory.Packages.props`; that belongs to its own effort with its own ADR.
