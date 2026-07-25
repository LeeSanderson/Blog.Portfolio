# Single shared Azure Function App for all portfolio apps

All portfolio apps' backend code compiles into one Azure Function App (`host/`), rather than each app deploying its own Function App resource. This keeps hosting cost and operational overhead low for a personal portfolio site, at the cost of a shared blast radius — a bug in one app's function can affect every other app's backend endpoints.

We mitigate the blast radius two ways: the full cross-app backend test suite must pass before every deploy (not just the touched app's tests), and app-scoped route prefixes (`/api/{app-name}/...`) are enforced via an architecture test to prevent route collisions between apps.

## Consequences

- A change to any single app's backend triggers a full rebuild and redeploy of the combined host.
- There is no way to deploy or roll back one app's backend independently of the others.

## Amendment: CD is a manual trigger, not auto-deploy-on-green

The original intent (spec item 21) was for a green CI run on `main` to auto-deploy with no manual step. Ticket
06 splits this into `backend-ci.yml` (build + full test suite, runs on every commit) and a separate
`backend-cd.yml` that only runs on `workflow_dispatch` — Lee decided, while ticket 06 was being implemented, to
keep deploys manual for now rather than wire up auto-deploy immediately. CD still deploys the exact artifact
CI produced (no rebuild at deploy time) and still requires a green CI run to exist, so the full-test-suite gate
this ADR relies on for blast-radius mitigation is unchanged — only the trigger is manual instead of automatic.
Revisit switching `backend-cd.yml`'s trigger to `push`/`workflow_run` once auto-deploy is wanted again.
