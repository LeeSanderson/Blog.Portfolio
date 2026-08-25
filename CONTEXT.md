# Blog.Portfolio

A monorepo hosting a growing portfolio of small example and demo projects for sixsideddice.com, sharing one deployable backend and a common set of local-dev and delivery conventions.

## Language

**App**:
A single portfolio project (e.g. a game, a tool, a demo) living under `apps/{app-name}/`. An app may have a `backend/`, a `frontend/`, or both. It is a deployment/portfolio unit, not necessarily a DDD bounded context — two apps may share the same underlying domain concepts.
_Avoid_: sub-application, domain, portfolio project

**Host**:
The single composition-root Azure Functions project that references every app's `backend/` project. It is the one deployable Azure Function App for the whole portfolio, and owns no app-specific business logic — only cross-cutting wiring.
_Avoid_: API, Function App (that term names the Azure resource; Host names the project that becomes it)

## Email Subscription

**Subscriber**:
A person who wants to be emailed about new sixsideddice.com blog posts, identified by email address. Moves through three states: Pending (submitted, not yet confirmed) → Active (confirmed, receives the weekly digest) → Unsubscribed (opted out). Confirming and unsubscribing are unconditional, idempotent writes — whichever action was performed most recently wins, regardless of the Subscriber's prior state (e.g. clicking an old confirm link after already unsubscribing moves them back to Active; there is no guard requiring Pending before confirming).
_Avoid_: Subscription, Contact, User

**Digest**:
The email that lists new sixsideddice.com blog posts, rendered from whatever set of posts it is handed. Its cadence is a separate concern: the seven-day window and the Monday 08:00 UTC send belong to the timer that triggers it, which is why `DigestEmailBuilder` renders the email while `WeeklyDigestFunction` decides when one goes out.
_Avoid_: Weekly digest (as a name for the email itself), Newsletter, Roundup
