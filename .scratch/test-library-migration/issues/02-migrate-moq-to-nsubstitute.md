# 02 — Migrate Moq to NSubstitute

**What to build:** Translate the five test files that mock, from Moq to NSubstitute 6.2.0, one construct at a
time with no change to what any test asserts. Moq is removed from the repo entirely. `NSubstitute.Analyzers.CSharp`
comes along to guard the one way NSubstitute is genuinely more dangerous than Moq.

**Blocked by:** 01 — this ticket's proof is "the suite still passes", so the assertion library must not be
moving underneath it. See `spec.md` for the full ordering argument.

**Status:** done

- [x] `Directory.Packages.props` drops `Moq` and adds `NSubstitute` 6.2.0 and
      `NSubstitute.Analyzers.CSharp` 1.0.17
- [x] The email-subscription test project references both, the analyzer with `PrivateAssets="all"` so it stays
      build-time only. No other project gains a mocking reference — none of them mock.
- [x] All five test files translated: Subscribe, Confirm, Unsubscribe, WeeklyDigest, SendEmail
- [x] `CODING_STANDARDS.md` and both `README.md` mentions name NSubstitute
- [x] The `tdd` skill's Moq sample in `tests.md` is translated to NSubstitute and remains the **BAD** example,
      and `mocking.md` gains a passage distinguishing owned ports over external infrastructure from genuine
      internal collaborators
- [x] `docs/adr/0011-nsubstitute-for-mocking.md` records the decision and the explicit no-s
- [x] The full suite is green with the **same test names and the same test count**, and all 26 verifications
      still assert exact counts

## Translation

| Moq | NSubstitute |
| --- | --- |
| `Mock<T> _x = new()` plus `_x.Object` | `T _x = Substitute.For<T>()` |
| `.Setup(x => x.M(a)).ReturnsAsync(v)` | `_x.M(a).Returns(v)` |
| `It.IsAny<T>()` | `Arg.Any<T>()` |
| `It.Is<T>(p)` | `Arg.Is<T>(p)` |
| `.Verify(x => x.M(a), Times.Once)` | `_x.Received(1).M(a)` |
| `.Verify(x => x.M(a), Times.Never)` | `_x.DidNotReceive().M(a)` |

`Times.Once` maps to `Received(1)`, never to bare `Received()` — that means "at least once" and would silently
weaken all twelve exact-count assertions.

## Three places to be careful

1. `ReturnsAsync(default(Subscriber))` becomes `Returns((Subscriber?)null)`.
2. `SubscribeFunctionTests.HandleAsync_AlwaysReturnsTheSameGenericMessageRegardlessOfPriorState` configures
   the same call twice and relies on the second configuration winning. Moq and NSubstitute both take the most
   recent matching configuration, but the mechanism differs, so this test is worth re-reading after
   translation rather than trusting the pattern.
3. NSubstitute auto-substitutes interface return types, so an un-arranged `GetPostsAsync` or
   `ListByStatusAsync` yields a non-null, empty-behaving substitute rather than Moq's default. Every test that
   reaches those paths already arranges them, so this should not bite — but it is the first place to look if
   something goes red unexpectedly.

## Why the analyzer

Moq's `x => x.Method(…)` lambda is never executed, so a malformed setup is a compile error. NSubstitute's
syntax is a real method call on a real object, so a mistake usually compiles and silently does nothing:

```csharp
_emailOutbox.Received(1);                    // compiles, asserts nothing, test passes
_emailOutbox.Received(1).EnqueueAsync(…);    // what was meant
```

There are 26 verification sites here. A silently vacuous assertion is worse than a broken one, because nothing
goes red to tell you. NS5000 catches exactly this at build time.

## Comments

**Verified:** 29 tests green, same names and same count as the pre-change baseline. The verification profile is
unchanged — 12 `Received(1)` and 14 `DidNotReceive()`, exactly matching the 12 `Times.Once` and 14
`Times.Never` they replaced — with no bare `Received()` anywhere, which would have weakened an exact-count
assertion into "at least once". No `Moq`, `It.Is`, `Times.` or `.Object` remains in any test file.

**NS5000 was proved live rather than assumed**, following the same discipline ADR-0010 applied to the snapshot
guard. A deliberately dangling `emailSender.Received(1);` was added to `SendEmailFunctionTests`, and the build
failed with `error NS5000: Unused received check` — an error, not a warning. The probe was then reverted.

**One deviation from the planned translation.** `ReturnsAsync(default(Subscriber))` was to become
`Returns((Subscriber?)null)`, but that fails the build under `MA0181: Do not use cast`. Replaced with
`ReturnsNull()` from `NSubstitute.ReturnsExtensions`, which is NSubstitute's own idiom for the case, needs no
cast, and reads better than the original. This is the only place the translation table in this ticket does not
describe what actually landed.

**Outcome of the three flagged risks:** the double-configuration in
`HandleAsync_AlwaysReturnsTheSameGenericMessageRegardlessOfPriorState` behaves as expected — the later, more
specific configuration wins, and the test passes. The auto-substitution of interface return types on
un-arranged `GetPostsAsync`/`ListByStatusAsync` calls never came into play, since every test reaching those
paths arranges them. Neither needed a workaround.
