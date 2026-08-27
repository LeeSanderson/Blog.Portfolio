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
Lit, Vite, Vitest and `Aspire.Hosting.NodeJs`, so ADR authoring is in the destination's
scope. Avoid code comments; let names carry intent.

**Settled at charting** (from the `/grilling` session that produced this map — these are
constraints on every ticket, not open questions):

| | Decision |
|---|---|
| Seam | This repo owns whole pages *and* the widget bundle, deployed to `/subscribe/` on `leesanderson.github.io` via the existing `_reusable-frontend-deploy.yml` (ADR-0002) |
| Stack | Lit + Vite + Vitest/happy-dom; npm enters the monorepo as dev tooling |
| Widget state | Four states in `localStorage`: pending / subscribed / unsubscribed / dismissed. Opt-out is permanent; dismissal ages out |
| Surfaces | `/subscribe/`, `/subscribe/confirm/`, `/subscribe/unsubscribe/`, `/subscribe/widget.js`; one shared form component |
| Local dev | Aspire `AddNpmApp` resource so `./run-local.ps1` runs the whole stack; the resource failing to start in CI is accepted noise; `npm run dev` still works standalone |
| Local email | Real Resend sends, gated on verifying the sending domain |
| API URL | Repo variable `API_BASE_URL` + a new generic `build-env` input on the reusable workflow; Vite bakes `VITE_API_BASE_URL` |
| Testing | Vitest + happy-dom, no browsers. Scroll trigger sits behind a pure `shouldPrompt()` seam because happy-dom has no layout engine |
| Styling | Light-DOM Lit following the `six-sided-*` precedent, inheriting Bootstrap Darkly; `bootstrapdarkly.min.css` + `site.css` vendored for the pages and local dev |
| Blog contract | One `<script type="module">` line in the BlogToHtml post template; the widget self-injects |

Two edges named at charting rather than buried:

- Choosing real Resend sends makes the whole local confirm/unsubscribe loop depend on
  [Verify the sixsideddice.com sending domain in Resend](issues/03-verify-resend-sending-domain.md).
  If that stalls, a dev-only file-drop `IEmailSender` is the unblock-yourself fallback and can
  be added later without disturbing anything else.
- Light DOM buys a native look at the price of coupling: a Bootstrap upgrade or a `site.css`
  edit on the blog can restyle or break the widget with no signal in this repo, and no test
  here will catch it.

## Decisions so far

<!-- one line per closed ticket: gist + link -->

- [Name the reader's local subscription state](issues/01-name-the-readers-local-state.md) — the
  browser-local concept is a **Signup Record** (now in `CONTEXT.md`), stated as
  `submitted`/`confirmed`/`optedOut`/`dismissed` so it shares no word with the Subscriber; stored at
  `six-sided.signup` as `{v, state, at}` with no email, anything unrecognised treated as no record;
  `dismissed` ages out at 30 days and `submitted` at 7, `confirmed`/`optedOut` never, as build-time
  constants; both edge cases accepted, with an "I'm already subscribed" control mitigating
  per-browser state.

## Not yet specified

- **Error, empty and offline states, and their copy.** What the form says when the API is
  unreachable, when it returns a non-200, and what the confirm page shows for a bad or
  tampered signature. Takes shape once the prototypes exist.
- **Accessibility bar.** Focus management, `aria-live` on the result messages, honouring
  `prefers-reduced-motion`, and what the landing pages show with JavaScript disabled. Its
  shape depends on which widget presentation wins.
- **Keeping the vendored Bootstrap Darkly copy honest.** How the copied CSS stays in step
  with the blog, and what would signal that it has drifted.
- **Analytics.** Whether subscribe/confirm/dismiss feed the blog's existing Google Analytics
  tag, and whether that is wanted at all.
- **Non-article pages.** What the widget does if the script is ever loaded somewhere without
  an article to anchor to.

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
- **The apex domain's broken HTTPS.** Surfaced while resolving
  [Name the reader's local subscription state](issues/01-name-the-readers-local-state.md):
  `http://sixsideddice.com` 301s to `www`, but `https://sixsideddice.com` fails TLS, and the backend's
  CORS allowlist includes that effectively-dead origin. Blog infrastructure, not this effort.
