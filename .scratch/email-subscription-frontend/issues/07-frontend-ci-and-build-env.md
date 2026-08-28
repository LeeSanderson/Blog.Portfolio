# 07 — Frontend CI and the build-env channel

Type: grilling
Status: resolved
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

## Answer

**The ticket's central question — the shape of a generic `build-env` input — was dissolved rather
than answered.** Readability was made the driving criterion, and the seam was re-cut so each app's
frontend workflow owns its own install, build and test. Once the app owns its build there is no
generic channel to design: the build step simply sets
`env: VITE_API_BASE_URL: ${{ vars.API_BASE_URL }}`, like any ordinary workflow.

`_reusable-frontend-deploy.yml` had **zero real callers** — only its own template and README
referenced it — so re-cutting the seam cost nothing but the file.

### The shape: one linear job, two composite actions

The deciding mechanical fact: a **reusable workflow** runs as its own job with its own filesystem, so
handing it `dist/` needs an artifact round-trip; a **composite action** runs inside the calling job and
sees `dist/` on disk. Composite actions win, and not only for readability — in any two-job shape the
deploy job re-runs `npm ci && npm run build`, so **the bytes that reach the blog were never the bytes
the tests ran against**. One job makes tested and shipped the same build. (The backend's
CI→CD artifact handoff exists because its CD is a separate manual workflow that may deploy days later;
the frontend deploys in the same run, so an artifact would buy only plumbing.)

- **`.github/actions/publish-to-pages/`** — inputs `source-dir`, `target-path`, `token`. Checks out
  the target repo, copies, commits, pushes. The PAT arrives as a `with:` input because the `secrets`
  context is **not available to composite actions** (confirmed in the GitHub docs); masking follows
  the value, not the channel, so it is still redacted in logs.
- **`.github/actions/smoke-test-api/`** — a single input, `base-url`. It owns
  `/api/example/ping`, the `"message":"pong"` assertion and the 10 × 10s retry policy as constants.

`smoke-test-api` deliberately takes the origin as an input rather than reading `vars.API_BASE_URL`
itself, and the reason is not convenience: **the two callers must be able to pass different origins,
because a difference between them is the entire signal.** If the action read the variable, backend
CD's smoke test would stop testing the deployment it just made and start testing whatever the
variable claims — going green against a stale-but-alive host while the actual new deployment is
broken.

```yaml
name: Email subscription frontend

on:
  push:
    paths:
      - "apps/email-subscription/frontend/**"
      - ".github/workflows/email-subscription-frontend.yml"
      - ".github/actions/publish-to-pages/**"
      - ".github/actions/smoke-test-api/**"
  workflow_dispatch: {}

concurrency:
  group: pages-deploy
  cancel-in-progress: false

defaults:
  run:
    working-directory: apps/email-subscription/frontend

jobs:
  build-test-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-node@v7
        with:
          node-version: "22"
          cache: npm
          cache-dependency-path: apps/email-subscription/frontend/package-lock.json

      - run: npm ci
      - run: npm test
      - run: npm run build           # postbuild guard runs here
        env:
          VITE_API_BASE_URL: ${{ vars.API_BASE_URL }}

      - uses: ./.github/actions/smoke-test-api
        if: github.ref == 'refs/heads/main'
        with:
          base-url: ${{ vars.API_BASE_URL }}

      - uses: ./.github/actions/publish-to-pages
        if: github.ref == 'refs/heads/main'
        with:
          source-dir: apps/email-subscription/frontend/dist
          target-path: subscribe
          token: ${{ secrets.PAGES_DEPLOY_TOKEN }}
```

The gate is **structural** — a failed step aborts the job, so publish cannot run behind a red test.
No `branches:` filter, matching `backend-ci.yml`: tests run on every branch push and only the publish
steps are gated to `main`, so a branch filter can never hide a break until merge. `workflow_dispatch`
is the escape hatch for redeploying after fixing a repo variable, with no code change.
`concurrency: pages-deploy` is deliberately **shared across every frontend app**, not per-app,
because the contention is on the target repo.

Note `target-path: subscribe`. The deleted template hard-wired `target-path: <app-name>`, which would
have published to `/email-subscription/` — not the `/subscribe/` every other ticket on this map
assumes.

### The API URL: a repository variable, `API_BASE_URL`, holding the full origin

Follows the house rule already in `infra/README.md` (non-sensitive → repository variable via
`vars.*`), with `EMAIL_SUBSCRIPTION_FROM_ADDRESS` as the existing precedent. Not a secret, for two
reasons beyond the rule: the value is *definitionally* public, since it is compiled into a JS bundle
served off a public blog; and masking would **actively hurt**, because a secret is redacted in logs,
so the build log could never show which URL got baked in — removing the only cheap diagnostic for
precisely the invisible failure this map keeps collecting.

It holds the **full origin**, not the bare hostname, because `https://${{ vars.X }}` in YAML is
fail-silently-shaped: set the variable to a full URL by mistake and you get `https://https://…`, a
widget that talks to nowhere, and no signal. Storing exactly what is consumed removes the transform.
The cost is two names for one fact, so provenance needs a line in `infra/README.md`: it is `https://`
plus the `AZURE_FUNCTION_HOSTNAME` value from `azd env get-values`.

### Staleness: the ticket's premise was wrong, and the risk is much narrower

The hostname is `func-${take(envName,46)}-${resourceToken}`, where `resourceToken` is 8 chars of
`uniqueString(subscription().id, environmentName, location)` — and **`uniqueString` is deterministic
on its inputs**. So deleting and recreating the azd environment *under the same name, subscription and
location* yields the **same** hostname. The ticket's "recreating the azd environment changes it" only
holds if the env **name**, **location** or **subscription** changes — a rare, deliberate act that
already requires editing the `AZURE_ENV_NAME` / `AZURE_LOCATION` repo variables.

What remains is two *distinct* failures, each getting one cheap check:

| Check | What it actually catches |
|---|---|
| `smoke-test-api` in the frontend workflow, before publish | A **typo** in `API_BASE_URL`, a stopped or deleted Function App, a wrong-env URL. Fires at the moment of shipping. |
| A comparison in `backend-cd.yml`: fail if `vars.API_BASE_URL` is set and does not match the provisioned `AZURE_FUNCTION_HOSTNAME` | The env was genuinely renamed or moved. Fires at the moment of **breakage**, with no frontend push needed. |

The typo case is far likelier than the staleness case and nothing else catches it, which is why the
ping is the essential half. The comparison must **skip when the variable is unset** rather than fail —
unset is the bootstrap state, and it is already caught loudly by the build guard below. Both live
beside a hostname `backend-cd.yml` already extracts for its own smoke test:

```yaml
      - name: Resolve provisioned hostname
        id: hostname
        run: |
          set -euo pipefail
          hostname=$(azd env get-values | grep '^AZURE_FUNCTION_HOSTNAME=' | cut -d= -f2- | tr -d '"')
          echo "hostname=$hostname" >> "$GITHUB_OUTPUT"

      - uses: ./.github/actions/smoke-test-api
        with:
          base-url: https://${{ steps.hostname.outputs.hostname }}

      - name: Check the frontend's baked API URL still matches
        if: vars.API_BASE_URL != ''
        run: |
          set -euo pipefail
          expected="https://${{ steps.hostname.outputs.hostname }}"
          actual="${{ vars.API_BASE_URL }}"
          [ "$expected" = "${actual%/}" ] || {
            echo "API_BASE_URL is $actual but the provisioned host is $expected" >&2
            echo "Every form on the live blog is now pointing at the wrong host." >&2
            exit 1
          }
```

Backend CD adopting `smoke-test-api` is not incidental: it replaces a hand-rolled 10 × 10s retry loop
(`backend-cd.yml` lines 81–93), so the repo carries one implementation instead of two — and the shared
action gets proven by the workflow that runs most often, rather than only by the frontend rail this
ticket exists because it has never run.

### Guarding the built widget: a `postbuild` script, not a CI step

```json
"scripts": { "build": "vite build", "postbuild": "node scripts/verify-dist.mjs" }
```

`postbuild` beats a workflow step on four counts: it is a standard npm lifecycle hook, so nobody can
forget to invoke it; it runs on a developer's laptop, not just in CI; the shared workflow and both
composite actions learn nothing about `widget.js`; and one script covers every trap at once.

| Assertion | Trap it catches |
|---|---|
| `dist/widget.js` exists, `dist/widget.mjs` does not | missing `"type": "module"` → the blog's hard-coded URL 404s |
| bundle contains the literal `process.env.VITE_API_BASE_URL`, which must itself be non-empty | a `--mode` switch compiling the URL to `void 0` — **and** an unset or empty `API_BASE_URL` repo variable |
| no `dist/widget.css` emitted | `import './widget.css'`, which a bare `<script>` never fetches |
| no surviving bare-specifier imports (`from "lit"`) | missing `consumer: 'client'` leaving Lit external |

The first three are [Vite build shape](04-vite-build-shape.md)'s three named traps; the fourth is its
"undocumented bit", which rests on empirical behaviour found on no docs page and is therefore the most
likely to regress on a Vite upgrade. The second assertion reads the expected origin from the
environment rather than hard-coding it, so the script needs no configuration and doubles as the loud
failure for an unset repo variable.

A **gzip size ceiling** was weighed as a fifth assertion and **declined**: the "no bare imports" check
already covers the externalised-Lit case, and a budget is the one assertion needing periodic tuning
that will eventually cry wolf.

### The AppHost's npm install: remove the resource in the test

Ticket 05 handed over three options; a fourth was found and chosen. `IDistributedApplicationTestingBuilder.Resources`
**does exist at 13.4.6** — verified against the package on disk, not assumed — so
`ExamplePingEndToEndTests` can remove the `frontend` resource before `BuildAsync`.

The argument is coverage, not speed: `GetExamplePing_ReturnsPongThroughTheRunningAppHost` tests a
backend Function through Aspire and has **no dependency on a Vite dev server**. Starting one, and
npm-installing to do it, is incidental cost with zero coverage value. A test should start the resources
it depends on. It is also the only option that keeps both properties the others trade away — backend CI
stays fast and lockfile-safe, while `./run-local.ps1` keeps its auto-install so local dev still works
from a fresh clone.

A cost ticket 05 observed but did not weigh, and which sharpens the case: Aspire runs **`npm install`,
not `npm ci`** — it created `package-lock.json`. So the leak does not merely cost 13–33s, it can
**mutate the committed lockfile**. Locally that shows up in `git status`; in CI it is invisible.

The three rejected options, for the record:

- **Accept** — invisible lockfile mutation on every backend run touching `aspire/**`. (The npm-registry
  dependency would *not* have been new: backend CI already npm-installs Core Tools.)
- **`WithNpm(install: false)`** — the frontend resource goes to `Finished`, so `./run-local.ps1`
  silently comes up without a frontend for anyone who has not run `npm install`. A new silent failure,
  on a map that keeps naming them.
- **Narrowing `backend-ci.yml`'s path filter** — AppHost changes would stop running backend tests. The
  AppHost *is* backend code and the E2E test lives on it; a real coverage regression.

### Deletions, and no template

`_reusable-frontend-deploy.yml` **and** `.github/workflows/templates/` both go, rather than the
template being rewritten. It was written speculatively, never executed, and this one session found it
wrong in three separate ways: no test gate at all (contradicting ADR-0002's own stated safety net of
"the build/test gate in the app's own CI workflow"), `target-path: <app-name>` publishing to the wrong
path, and action pins two majors stale on a deprecated Node runtime. A template that has never run is a
liability — it encodes untested guesses. The real, working `email-subscription-frontend.yml` is the
better exemplar, and "copy it" is a fine instruction.

The README's genuinely useful prose — that `PAGES_DEPLOY_TOKEN` is shared across all apps rather than
per-app, plus the ADR-0002 rationale — moves to a README beside `publish-to-pages`, next to the thing
that actually needs the PAT.

### Action versions: the whole repo is one line stale

Latest majors read from the GitHub releases API, and `setup-node@v7`'s inputs confirmed against the
`v7.0.0` tag's own `action.yml` rather than its release notes: `cache` and `cache-dependency-path`
both survive unchanged, and the action now runs on **`node24`** — which is exactly the deprecation
`@v4` carries (node20).

Current majors: `checkout` **v7**, `setup-node` **v7**, `upload-artifact` **v7**,
`download-artifact` **v8**, `setup-dotnet` **v6**, `setup-azd` **v2**.

The repo has exactly nine `uses:` lines. `backend-ci.yml` is already fully current; the only two files
carrying stale pins were the reusable workflow (deleted) and `backend-cd.yml`. So the sweep is **one
line**: `backend-cd.yml:32`, `actions/checkout@v4` → `@v7`. New files start on current majors.

`setup-node@v7` also adds a `package-manager-cache` input that auto-enables caching when `package.json`
declares `packageManager`; write `cache: npm` explicitly rather than relying on it.

### The loose end, settled

`publish-to-pages` **fails** on a non-fast-forward push rather than rebasing and retrying. The push
either happened or it did not, so failure leaves nothing half-broken; `workflow_dispatch` is a
one-click retry; and it keeps rebase logic out of a shared action.

### Named scope creep

This ticket edits `backend-cd.yml` — the staleness comparison, adopting `smoke-test-api`, and the
stale `checkout@v4`. That is backend delivery, not frontend, and outside this map's stated destination.
Accepted because the comparison check has to live there regardless, so the file is being opened anyway.

### To confirm at implementation, not decidable here

Whether removing the `frontend` resource also takes the auto-spawned `frontend-installer` with it, or
whether that must be removed too. If the installer cannot be removed, the fallback is accepting the
leak and documenting it.
