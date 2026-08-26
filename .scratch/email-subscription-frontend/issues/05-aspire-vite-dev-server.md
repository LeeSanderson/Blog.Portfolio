# 05 — Aspire hosting for the Vite dev server

Type: research
Status: open
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
