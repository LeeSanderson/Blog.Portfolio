# 06 — Decide whether to migrate the solution to xunit v3

**What to decide:** The snapshot tooling ticket 04 introduced sits on a deprecated, frozen package. Lee needs
to decide whether to migrate the solution from xunit 2.x to xunit v3, or to stay put and accept that the
snapshot tooling no longer receives updates.

**Blocked by:** nothing — 04 and 05 are done and green on this package set.

**Status:** needs-triage

This is a decision ticket, not an implementation ticket. It exists because ticket 04 said to raise the xunit
v3 upgrade with Lee rather than perform a solution-wide migration silently, and the trigger it named has
partly fired.

## What was found

- `Verify.Xunit` is the xunit **v2** package. It was deprecated on 2026-02-12 with reason "Legacy" and
  `Verify.XunitV3` named as its alternate.
- It is frozen at **31.12.5** (published 2026-02-11) while Verify's core package has continued to
  **31.28.0**. 31.12.5 is its final version.
- It is MIT, listed, and works — ticket 04 is green on it, and no migration was needed to land the snapshots.

So xunit v2 support is **deprecated, not dropped**, which is why ticket 04 proceeded rather than stopping.
The letter of its guard ("if it has been dropped, stop") was not met.

## Why it still needs a decision

The consequence of the freeze is already being paid, one ticket after the tooling landed:

- `Verify.DiffPlex` is pinned to **3.1.2** rather than the current 3.3.1, because 3.3.1 requires Verify core
  31.24.0+, which is **binary incompatible** with the frozen `Verify.Xunit`. The incompatibility surfaces as
  a runtime `MethodAccessException` inside the test run, not as a build error — a failure mode that will read
  as baffling to whoever next bumps that package without knowing why it is held.
- Every future Verify improvement is now unreachable without the migration.

## The options

1. **Migrate to xunit v3** — swap `Verify.Xunit` for `Verify.XunitV3`, move all five test projects to xunit
   v3, unpin `Verify.DiffPlex`. Solution-wide, touches every test project, deliberately out of scope for 04.
2. **Stay on v2** — accept frozen snapshot tooling and the DiffPlex pin. Costs nothing today; the pin comment
   in `Directory.Packages.props` and ADR-0009 are what stop it becoming a mystery later.

## Context

- ADR-0009 records the tool choice and both caveats.
- The pin and its reason are commented at the `Verify.DiffPlex` / `Verify.Xunit` entries in
  `Directory.Packages.props`.
- Ticket 04's `## Comments` section records the full verification result.
