# 08 — Write the spec and implementation tickets

Type: task
Status: open
Blocked by: 01, 02, 03, 04, 05, 06, 07, 09, 10, 11

## Question

The terminal ticket — resolving it *is* reaching the destination. Nothing to decide by then;
every decision is already recorded on this map or in a closed ticket. This is the write-up.

Produce:

- `.scratch/email-subscription-frontend/spec.md`, in the shape of
  `.scratch/email-subscription/spec.md`: Problem Statement, Solution, User Stories,
  Implementation Decisions, Testing Decisions, Out of Scope, Further Notes. Use `/to-spec`.
- Numbered implementation tickets under `issues/` — but note the collision: this directory
  already holds this map's decision tickets numbered `01`–`08`. Decide where the implementation
  tickets live so the two sets are not confused, and say so in the spec.
- **The ADRs.** This effort adds Lit, Vite, Vitest and `Aspire.Hosting.NodeJs`, and changes a
  shared reusable workflow. Per the standing bar, each new tool gets an ADR even where it is
  cheaply reversible — decide whether that is one "frontend tooling stack" ADR or several, and
  write them into `docs/adr/` continuing from `0012`.
- **`CONTEXT.md`.** Carry in the vocabulary settled by
  [Name the reader's local state](01-name-the-readers-local-state.md), if that ticket did not
  already land the edit.
- **`CODING_STANDARDS.md`.** Its Project layout section describes backend conventions only. Decide
  whether frontend conventions (component naming, where tests live, light-DOM Lit) belong there
  now or wait until a second frontend exists to generalise from.

Two things the spec must carry explicitly, because they are the parts most easily lost:

- **The BlogToHtml contract.** The exact `<script type="module">` line to add to the post template
  in `C:/Dev/Personal/Blog`, written out verbatim, plus the note that the edit itself is out of
  scope for this repo. Without it, the widget ships and never appears anywhere.
- **`README.md` for the app.** `apps/README.md` says every app may have its own; this becomes the
  first frontend in the monorepo, so how to run and test it should not live only in a spec under
  `.scratch/`.
