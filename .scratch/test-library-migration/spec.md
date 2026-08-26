# Test Library Migration — NSubstitute for mocking, AwesomeAssertions for assertions

Status: needs-triage

Both implementation tickets are delivered. The only outstanding item is ticket 03, a decision ticket that was
deliberately left unstarted.

## Problem Statement

Two of the four libraries in this repo's test stack are on packages Lee would rather not build on.

`FluentAssertions` is at 8.10.0. The 8.0 release moved from Apache-2.0 to the Xceed Community License, which
is free for non-commercial use but charges for commercial use; 7.x remains Apache-2.0. `AwesomeAssertions` is
an Apache-2.0 community fork of the 7.x line whose maintainers have committed to never relicensing.

`Moq` is at 4.20.72 and works fine. NSubstitute is simply Lee's preferred mocking library. The reason to act
now rather than later is that the switching cost only grows: Moq appears in five test files today, and every
test written between now and a future migration adds to that number. The argument for moving is at its
strongest the day it is cheapest.

## Solution

Two mechanical, independently-verifiable swaps, delivered as sequential tickets, plus one deferred question.

Ordering matters and is deliberately the reverse of the order these were decided in. **Ticket 01
(AwesomeAssertions) goes first** because it is purely mechanical — a package swap and nine `using` lines, with
every possible error caught by the compiler and no semantic content whatsoever. **Ticket 02 (NSubstitute)
follows** because it is the opposite: `Times.Once` becomes `Received(1)`, so assertion *semantics* are being
retranslated and a passing suite is the only thing proving the translation was faithful. That oracle needs to
stand on a baseline that is not moving underneath it. Done the other way round, the assertion library would be
swapped beneath five freshly-rewritten test files, and any failure would be ambiguous between a bad Moq
translation and an assertion-library difference.

Each ticket therefore carries an acceptance property that isolates it:

- **Ticket 01** — full suite green, **no test body changed**, every `*.verified.html` byte-identical.
- **Ticket 02** — full suite green, **same test names and count**, all 26 verifications keeping exact counts.

Neither ticket restructures a test. Both are translations.

## Scope

In scope:

- `Directory.Packages.props` and the four test project files
- The nine source files using FluentAssertions and the five using Moq
- `NSubstitute.Analyzers.CSharp`, which guards NSubstitute's one genuine weakness against Moq: its arrange
  and assert syntax are real method calls, so a dangling `_x.Received(1);` with no following member call
  compiles, asserts nothing, and passes. There are 26 verification sites where that could happen.
- Documentation: `CODING_STANDARDS.md`, `README.md`, and the `tdd` skill's mocking guidance

Out of scope:

- Restructuring any test — see ticket 03
- Any guard against Moq returning beyond what Central Package Management already gives: with no
  `PackageVersion` entry, a `PackageReference` to Moq fails the build (NU1010)
- Global usings for either library. `<Using Include="Xunit" />` earns its place because `[Fact]` is in every
  test file; NSubstitute is in 5 of 12, and there `using NSubstitute;` at the top of a file is signal worth
  keeping — it says at a glance that this test mocks something.
- Microsoft.Testing.Platform and xunit 4.x, both still parked per ADR-0010
- The `.scratch/` specs that mention Moq and FluentAssertions. They are historical records of what was true
  when written, not live documentation.

## Decisions

The reasoning behind both choices, and the alternatives rejected, are recorded as ADRs rather than restated
here:

- `docs/adr/0011-nsubstitute-for-mocking.md`
- `docs/adr/0012-awesomeassertions-for-assertions.md`

## Tickets

- **01** — `01-switch-to-awesomeassertions.md` — **done**
- **02** — `02-migrate-moq-to-nsubstitute.md` (was blocked by 01) — **done**
- **03** — `03-consider-in-memory-fakes.md` — `needs-triage`, deliberately unstarted
