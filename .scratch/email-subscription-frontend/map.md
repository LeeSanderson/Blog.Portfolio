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
| Seam | This repo owns whole pages *and* the widget bundle, deployed to `/subscribe/` on `leesanderson.github.io` via the existing `_reusable-frontend-deploy.yml` (ADR-0002) |
| Stack | Lit + Vite + Vitest/happy-dom; npm enters the monorepo as dev tooling |
| Widget state | Four states in `localStorage`. Opt-out is permanent; dismissal ages out. *The placeholder names chosen here were replaced by `submitted`/`confirmed`/`optedOut`/`dismissed` — see [Name the reader's local subscription state](issues/01-name-the-readers-local-state.md)* |
| Surfaces | `/subscribe/`, `/subscribe/confirm/`, `/subscribe/unsubscribe/`, `/subscribe/widget.js`; one shared form component |
| Local dev | An Aspire resource so `./run-local.ps1` runs the whole stack; `npm run dev` still works standalone. *The call is `AddViteApp` from `Aspire.Hosting.JavaScript` — `AddNpmApp`/`Aspire.Hosting.NodeJs` named here at charting do not exist at 13.4.6. And the resource does **not** fail to start in CI: Aspire 13 installs the dependencies instead, which is a quieter problem, not accepted noise — see [Aspire hosting for the Vite dev server](issues/05-aspire-vite-dev-server.md)* |
| Local email | Real Resend sends, gated on verifying the sending domain |
| API URL | Repo variable `API_BASE_URL` + a new generic `build-env` input on the reusable workflow; Vite bakes `VITE_API_BASE_URL` |
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
  here will catch it.
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

## Not yet specified

- **Error and empty states on the three pages, and their copy.** What the confirm page shows for a
  bad or tampered signature, and what either landing page shows when the API is unreachable. The
  widget's half of this is settled in
  [Prototype the article widget](issues/02-prototype-the-article-widget.md); the pages' half takes
  shape once [Prototype the three subscription pages](issues/06-prototype-the-subscription-pages.md)
  exists.
- **The three pages' accessibility**, in particular what they show with JavaScript disabled. The
  widget's half graduated to
  [The widget's accessibility bar](issues/09-widget-accessibility-bar.md); the pages' half waits on
  their prototype.
- **Keeping the vendored Bootstrap Darkly copy honest.** How the copied CSS stays in step
  with the blog, and what would signal that it has drifted.
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
