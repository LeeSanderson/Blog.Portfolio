# 06 — Decide whether to migrate the solution to xunit v3

**What to decide:** The snapshot tooling ticket 04 introduced sits on a deprecated, frozen package. Lee needs
to decide whether to migrate the solution from xunit 2.x to xunit v3, or to stay put and accept that the
snapshot tooling no longer receives updates.

**Blocked by:** nothing — 04 and 05 are done and green on this package set.

**Status:** done

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

## Decision

Lee chose **option 1 — migrate to xunit v3**. Recorded in `docs/adr/0010-xunit-v3-migration.md`.

## Comments

The migration surface turned out to be far smaller than "solution-wide, touches every test project" implied.
All four test projects use none of the APIs that changed between the xunit majors — no `Xunit.Abstractions`,
`ITestOutputHelper`, `IAsyncLifetime`, class or collection fixtures, or `MemberData` — so the change is
package references only, with no test code touched:

- `Directory.Packages.props`: `xunit` 2.9.3 → `xunit.v3` 3.2.2, `Verify.Xunit` 31.12.5 → `Verify.XunitV3`
  31.28.0, `Verify.DiffPlex` unpinned 3.1.2 → 3.3.1. Both pin comments removed.
- One `PackageReference` line per test project. `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`
  3.1.5 stay, so the projects remain on VSTest and `dotnet test`, coverlet, and CI are untouched.

One finding changed the shape of the answer: **xunit v3 is held at 3.2.2, not the current 4.0.0.**
`Verify.XunitV3` 31.28.0 is built against `xunit.v3.extensibility.core` 3.2.2, and 4.0.0 only shipped on
2026-08-15. That dependency is a NuGet minimum rather than an upper bound, so 4.0.0 would have resolved
without complaint — the hold is a precaution against an untested major pairing, not avoidance of a break
anyone has seen. It is worth taking because a Verify/framework mismatch is exactly what produced the
DiffPlex pin, and it surfaces as a runtime failure inside the suite rather than a version conflict at build.
The two packages now move together, and the reason is commented at the `xunit.v3` entry.

Two things were verified rather than assumed:

- **The approved files are byte-identical** before and after the migration (sha256 unchanged on both), which
  is the evidence that swapping the Verify adapter changed nothing about what the builders render.
- **The snapshot guard is still live.** `DigestEmailBuilder` was temporarily perturbed and the test failed
  with an inline DiffPlex diff in the console and no GUI window, then the probe was reverted. Worth doing
  because a snapshot suite that had silently stopped comparing would look exactly like a green one, and
  because it is the direct proof that DiffPlex 3.3.1 no longer throws `MethodAccessException`.

Full suite green on v3: 29 tests across the four projects, the same count as the pre-migration baseline.
