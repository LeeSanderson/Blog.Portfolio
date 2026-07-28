# example

A minimal walking-skeleton app. Not a real portfolio showcase — it exists to prove the `apps/{app-name}/` folder
convention, the shared REPR base class, and cross-assembly function discovery all work together, and to give
future apps a working template to copy.

## backend

`GET /api/example/ping` → `{ "message": "pong" }`, built on the `Endpoint<TRequest, TResponse>` REPR base from
`shared/backend/`. See `backend/src/Blog.Portfolio.Apps.Example.Backend/Ping/PingFunction.cs`.
