# 05 — Aspire hosting for the Vite dev server

Type: research
Status: resolved
Blocked by: —

## Question

The AppHost is three lines today (`aspire/Blog.Portfolio.AppHost/AppHost.cs`) — Azurite plus the
Functions Host, on Aspire SDK **13.4.6**. It needs a fourth resource for the Vite dev server, and
the details are version-sensitive enough to be worth reading rather than guessing.

Establish against the Aspire documentation for 13.x:

- The package that carries Node.js hosting on 13.4.6, and whether the right call is `AddNpmApp`,
  `AddViteApp`, or something newer. Both need adding to `Directory.Packages.props`, which is
  Central Package Management — so the version pin matters.
- How to pass the Host's resolved URL into the dev server's environment, so the frontend gets its
  API base URL from Aspire instead of a hard-coded `localhost:7240`. Confirm whether Aspire's
  service-discovery env vars reach a Node process in a usable form, or whether an explicit
  `WithEnvironment` referencing the host endpoint is the honest way.
- What actually happens when `node_modules` is absent — confirm the charting assumption that the
  resource lands in `FailedToStart` and that `app.StartAsync()` does not throw, since
  `ExamplePingEndToEndTests` waits only on `"host"` and CI will never install frontend deps.
  If that assumption is wrong, this is a live problem, not a footnote.
- Whether 13.4.6 offers `WithExplicitStart()` or an equivalent, which would silence the CI noise
  without an `npm ci` step — and what it costs locally (a resource that no longer starts with
  `./run-local.ps1` would defeat the point).
- Whether the dev server's port can be pinned, since the CORS allow-list in
  `host/src/Blog.Portfolio.Host/local.settings.json` names origins literally and today lists only
  `http://localhost:4000`.

Prefer primary sources: the Aspire docs and the package's own README for the matching version.
Capture findings with `/research`.

## Answer

**The charting assumption's conclusion holds, but its premise was wrong — and the correction moves
the risk rather than removing it.**

- **Findings**: branch `research/05-aspire-vite-dev-server`, commit `e49b467`, at
  `.scratch/email-subscription-frontend/research/05-aspire-vite-dev-server.md`. Largely
  **empirical** — three scenarios run against this repo's real AppHost, plus a dump of the actual
  Node process environment.
- Three load-bearing claims were re-verified independently against this repo before recording:
  the Aspire pin is genuinely `13.4.6`, `http://localhost:4000` **is already** in the
  `local.settings.json` CORS list, and `aspire/**` **is** in `backend-ci.yml`'s path filter.

### Q3 — the assumption, and what replaced it

The conclusion survives: `app.StartAsync()` does not throw, `host` still reaches `Running`, and
`ExamplePingEndToEndTests` still passes. **The reasoning behind it does not.** Aspire 13.0 turned on
automatic dependency install, so with `node_modules` absent Aspire does not leave the resource in
`FailedToStart` — it creates a second resource, `frontend-installer`, **runs `npm install`**, and the
dev server then starts normally. This was watched happening: `node_modules/` and `package-lock.json`
were created. Three scenarios were run — default (`frontend` → `Running`), `WithNpm(install: false)`
(→ `Finished`), and a deliberately failing install (→ `FailedToStart`) — and in **all three**
`StartAsync` did not throw, the host reached `Running`, and the test stayed green.

So there is no red build to worry about. The live risk is quieter and worse: `backend-ci.yml` already
provisions Node (it npm-installs Core Tools), and `aspire/**` is in its path filter, so **a
backend-only PR would silently run a real network `npm install` inside a backend test** — 13s
baseline rising to 18–33s with a warm cache. That is a CI decision, not a footnote, and it has been
handed to [Frontend CI and the build-env channel](07-frontend-ci-and-build-env.md).

### Q1 — the package the map names does not exist at this version

`Aspire.Hosting.NodeJs` is **dead**: it stops at 9.5.2, has no 13.x release, and NuGet carries
deprecation metadata saying it was renamed. The package is **`Aspire.Hosting.JavaScript` 13.4.6** —
not currently referenced, so it needs adding to `Directory.Packages.props`, where 13.4.6 matches the
`Aspire.Hosting.Azure.Functions` and `Aspire.Hosting.Testing` pins already there.
`AddNpmApp` no longer exists either — the call is
**`AddViteApp`**. Both the map's "Local dev" row and its "Standing preferences" paragraph named the
stale package and the stale method; both have been corrected.

### Q2 — service discovery does not reach client code

Explicit `WithEnvironment("VITE_API_BASE_URL", host.GetEndpoint("http"))` is the honest answer, and
this was settled by reading the real Node process environment rather than inferring. `WithReference`
*does* deliver `services__host__http__0=http://localhost:7240`, which is usable inside
`vite.config.js` — but Vite's transform output showed `import.meta.env` containing only
`VITE_API_BASE_URL`. Service-discovery variables are unprefixed, so they never reach client code.
Note the boundary: Aspire governs this in **dev only**; the built bundle still depends on the CI
`build-env` path.

### Q4 — `WithExplicitStart()` exists and does not help

It is present at 13.4.6, but the app resource sits at `NotStarted` while `frontend-installer` still
runs the install to completion. It buys nothing in CI and costs `./run-local.ps1` its startup — the
precise trade the ticket warned would defeat the point. If the implicit install is unwanted, the
lever is `WithNpm(install: false)`.

### Q5 — Aspire owns the port, and CORS needs no change

`AddViteApp` passes `--port <targetPort>` on the command line, which beats anything in
`vite.config.js`. Proved by conflict: with the config set to `port: 5555, strictPort: true` against
Aspire's 4000, Vite served on **4000** and 5555 refused connections. Use
`.WithHttpEndpoint(port: 4000, targetPort: 4000, isProxied: false)` — the docs advise against calling
this on a Vite resource, but 13.4.6 documents and implements it as *updating* the existing endpoint,
verified to neither throw nor duplicate.

**This ticket's answer contradicts an assumption in
[Frontend CI and the build-env channel](07-frontend-ci-and-build-env.md)**, which stated the Vite
origin "needs adding" to the local CORS list. It does not: `http://localhost:4000` is already
allow-listed, and nothing in either neighbour repo claims that port. Pinning Vite to 4000 makes the
CORS question disappear entirely. That ticket has been amended.

### Not established

Cold-cache CI install cost on `ubuntu-latest` against the real Lit/Vite tree; HMR websocket behaviour
and the `isProxied: true` path; behaviour with Node entirely absent; and `AddViteApp`'s publish-mode
Dockerfile generation — irrelevant today, but it would need `IsRunMode` guarding if the AppHost is
ever published. Note also that `aspire.dev` is unversioned and documents 13.5.x, so load-bearing
claims were re-confirmed against the `v13.4.6` tag or verified empirically.
