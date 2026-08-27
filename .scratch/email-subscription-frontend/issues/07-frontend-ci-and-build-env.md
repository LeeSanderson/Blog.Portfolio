# 07 — Frontend CI and the build-env channel

Type: grilling
Status: open
Blocked by: 04

## Question

This app is the first frontend in the repo, so it is the first real exercise of a delivery rail
that has never run: `.github/workflows/_reusable-frontend-deploy.yml` and its template were built
under ticket 07 of the monorepo-restructure effort and explicitly left unverified because no
frontend existed. Two gaps have to close before it can carry anything.

Blocked by [Vite build shape](04-vite-build-shape.md), because the install/build/test commands and
the build output directory are exactly what that ticket determines.

Settle:

- **The `build-env` input.** The reusable workflow passes no environment into its build step, so
  `API_BASE_URL` has no way in. Decide the input's shape — newline-delimited `KEY=VALUE`, JSON, or
  a typed input — bearing in mind this workflow is generic and shared by every future app, so it
  must not learn anything about email subscription.
- **Whether the value is a variable or a secret.** The Function App hostname is not sensitive, but
  it is also not public knowledge; decide `vars.API_BASE_URL` versus `secrets.` and how a reader
  of the repo knows where it came from (`azd env get-values`).
- **Staleness.** The hostname derives from `uniqueString(subscription().id, environmentName,
  location)`. Recreating the azd environment changes it and the repo variable silently goes stale,
  breaking every form on the live blog. Decide whether that is simply accepted and documented, or
  whether the deploy or the CD smoke test should catch it.
- **A frontend CI workflow.** `backend-ci.yml` is path-filtered to backend paths, so nothing runs
  the frontend's tests today. Decide whether a new `frontend-ci.yml` builds and tests on push,
  what its path filter is, and whether the deploy workflow gates on it or stands alone.
- **When deploys happen.** ADR-0002 chose direct push to `leesanderson.github.io` with no PR gate.
  Decide the trigger — every push to `main` touching the frontend, or manual dispatch like
  `backend-cd.yml` — and whether a broken build can reach the live blog.
- ~~**The local CORS origin.**~~ **Settled, no work.**
  [Aspire hosting for the Vite dev server](05-aspire-vite-dev-server.md) established that Aspire
  pins the dev server's port and that `http://localhost:4000` is *already* in the
  `local.settings.json` allow-list, so pinning Vite to 4000 means nothing needs adding. Production
  needs no change either — pages are served from `www.sixsideddice.com`, which
  `infra/functionapp.bicep` already allows.
- **The AppHost's npm install leaking into backend CI.** Handed over by
  [Aspire hosting for the Vite dev server](05-aspire-vite-dev-server.md), and the sharpest thing on
  this ticket. Aspire 13 auto-installs Node dependencies: when `node_modules` is missing it spawns a
  `frontend-installer` resource and runs a real `npm install`. Nothing goes red — but `backend-ci.yml`
  already provisions Node and lists `aspire/**` in its path filter, so **a backend-only PR would
  silently perform a network install inside a backend test**, measured at 13s → 18–33s warm-cache.
  Decide whether that is accepted, cut with `WithNpm(install: false)`, or avoided by narrowing the
  path filter. Note `WithExplicitStart()` was checked and does **not** help — the installer still
  runs to completion while the app resource waits at `NotStarted`.
- **Guarding the baked-in API URL.** Handed over by [Vite build shape](04-vite-build-shape.md): a
  mis-wired second build pass produces a widget that builds clean, emits no warning, and talks to
  nowhere, because `import.meta.env.VITE_API_BASE_URL` compiles to `void 0`. The suggested guard is
  a CI grep of `dist/widget.js` for the expected origin. Decide whether that check earns its place
  and where it runs — it is the only proposed signal for a failure that is otherwise invisible until
  a reader tries to subscribe.
