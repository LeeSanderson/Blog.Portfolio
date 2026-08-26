# 01 — Switch FluentAssertions to AwesomeAssertions

**What to build:** Replace `FluentAssertions` 8.10.0 with the Apache-2.0 fork `AwesomeAssertions` 9.6.0 across
all four test projects. A package swap and a namespace rename — no assertion is rewritten, no test body
changes, and the compiler catches every possible mistake.

**Blocked by:** None — goes first deliberately, because it is the purely mechanical of the two swaps and
ticket 02 needs a stable assertion baseline to verify itself against. See `spec.md`.

**Status:** done

- [x] `Directory.Packages.props` drops `FluentAssertions` and adds `AwesomeAssertions` 9.6.0
- [x] All four test projects reference `AwesomeAssertions` instead of `FluentAssertions`:
      email-subscription, example, `host/tests`, and the Aspire AppHost tests
- [x] The nine source files using the library import `AwesomeAssertions` instead of `FluentAssertions`,
      with the directive placed in alphabetical order rather than left where the old one sat
- [x] `CODING_STANDARDS.md`, `README.md` technologies list, and `README.md` testing section name
      AwesomeAssertions
- [x] `docs/adr/0012-awesomeassertions-for-assertions.md` records the licence position, the fork's Apache-2.0
      commitment, and the parity evidence
- [x] The full suite is green, **no test body has changed**, and every `*.verified.html` is byte-identical

## Notes

AwesomeAssertions 9.0.0 renamed the namespace from `FluentAssertions` to `AwesomeAssertions`. The 8.x line
(latest 8.2.0) kept the old namespace and would have made this a zero-source-change swap, but it is a
superseded major line — the same trap ADR-0010 documents for the frozen `Verify.Xunit` v2 line — and it would
leave `using FluentAssertions;` at the top of files that no longer depend on FluentAssertions.

The fork descends from FluentAssertions 7.x while this repo runs 8.10.0, so parity was checked rather than
assumed. The entire assertion surface across the solution is seven methods — `Be`, `BeFalse`, `BeTrue`,
`StartWithEquivalentOf`, `Throw`, `NotBeEmpty`, `Contain` — all stable across both lineages.

## Comments

**Verified:** 29 tests green, identical to the pre-change baseline (example 1, email-subscription 25,
`host/tests` 2, AppHost 1). The full diff is 16 files, 17 insertions, 17 deletions; every `.cs` change is the
one `using` line and nothing else; no `*.verified.html` was touched.

**Unrelated local problem surfaced on the way, and left alone deliberately.** `host/tests` failed to build on
`IDE0055: Fix formatting` at `RoutePrefixArchitectureTests.cs(107,115)` — a line this change does not touch.
The cause is that this working tree holds **bare LF** line endings for `.cs` files while `core.autocrlf=true`
expects CRLF on Windows. Files never touched by this change (`WeeklyDigestFunctionTests.cs`,
`ISubscriberStore.cs`, `CONTEXT.md`) are equally LF, so the condition predates it. `RoutePrefixArchitectureTests.cs`
is the only file that trips the analyzer, because its `#pragma warning disable MA0182` sits at column 1 inside
a class body: the formatter rewrites that region and emits the platform newline, which on Windows is CRLF. The
baseline was green only because the project had not been recompiled, and Linux CI is unaffected because there
the platform newline *is* LF.

Fixed by normalising that one file to CRLF, which `autocrlf` converts back on staging — so the committed diff
is still one line. **The wider working tree was left as it is.** Renormalising every file is a local
environment decision, not part of this ticket, and it would rewrite the entire tree on disk. Worth knowing
that any future edit to a `.cs` file containing a column-1 pragma will hit the same wall until it is dealt
with.
