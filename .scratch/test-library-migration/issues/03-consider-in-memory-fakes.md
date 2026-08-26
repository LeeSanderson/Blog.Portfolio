# 03 — Consider replacing the mocked stores with in-memory fakes

**What to decide:** Whether the five function tests should mock `ISubscriberStore` and `IEmailOutbox` at all,
or whether hand-written in-memory fakes would serve better — and separately, whether
`SendEmailFunctionTests` should exist in its current form.

**Blocked by:** 02 — there is no point rethinking the mocks until they have stopped moving.

**Status:** done

This is a decision ticket, not an implementation ticket. It exists because tickets 01 and 02 were deliberately
scoped as translations, so that a green suite would be proof the translations were faithful. Restructuring the
arrangements at the same time would have made a translation mistake and a deliberate redesign look identical
in the diff. The question was therefore parked rather than answered.

## Why this needs recording rather than leaving to be noticed

Ticket 02 adds a passage to the `tdd` skill's `mocking.md` explaining that an owned interface which is a port
over external infrastructure — `ISubscriberStore` over Azure Table Storage, `IEmailOutbox` over Azure Queue
Storage, `IEmailSender` over Resend — is a legitimate mocking boundary, and not the "internal collaborator"
the guidance warns against. That passage is correct, and it closes the trap door someone would otherwise have
fallen through when they next read those tests against the guidance. The reminder has to live somewhere, so it
lives here.

## The case for fakes

Every one of these tests arranges a store, calls one method, and verifies a call. A small in-memory
`ISubscriberStore` holding a dictionary would let the tests read as statements about behaviour — *given an
Unsubscribed Subscriber, when subscribing, the stored Subscriber is Pending* — asserted against real state,
rather than as `Arg.Any<CancellationToken>()` incantations asserted against call records. It would also remove
most of the 26 verification sites, and with them the class of mistake `NSubstitute.Analyzers.CSharp` was added
to catch.

## The case against

Fakes are code that has to be maintained and can itself be wrong, and a fake `ISubscriberStore` that quietly
diverges from the real Table Storage implementation is a worse failure than a mock that is obviously a mock.
The current tests are green, readable, and cheap. "Different" is not automatically "better".

## The one case that is not a judgement call

`SendEmailFunctionTests` is a single test over a single-line pass-through function, asserting that the
delegation happened, named after the mechanism rather than the behaviour:

```csharp
public async Task HandleAsync_SendsTheQueuedMessageViaTheEmailSender()
```

Measured against the `tdd` skill's own red flags — "asserting on call counts/order", "test name describes HOW
not WHAT" — this is the anti-pattern, and no clarifying passage about ports rescues it. Whatever is decided
about fakes generally, this test deserves its own answer: is there observable behaviour worth asserting here,
or should the test go?

## Decision

### Fakes: not adopted. The mocks stay.

Decided by Lee, 2026-08-26. The tests are green, readable and cheap; `Arg.Is<Subscriber>(s => s.Status ==
Active)` is not meaningfully worse than reading the status back off a dictionary; and a fake `ISubscriberStore`
that quietly diverges from the real Table Storage implementation is a worse failure than a mock that is
obviously a mock. `mocking.md`'s passage about ports over external infrastructure stands as the standing
answer for why these substitutions are legitimate.

Two findings from reading the five files closed the question rather than merely deferring it again.

**The ticket's framing was looser than the code.** Only two of the five files mock both `ISubscriberStore` and
`IEmailOutbox`; two mock the store alone, and one mocks neither:

| Test file | `ISubscriberStore` | `IEmailOutbox` | other |
|---|---|---|---|
| `SubscribeFunctionTests` | 6 stubs, 6 verifications | 5 verifications | — |
| `WeeklyDigestFunctionTests` | 1 stub, 2 verifications | 4 verifications | `IBlogFeedReader`, stub-only |
| `ConfirmFunctionTests` | 2 stubs, 4 verifications | not used | — |
| `UnsubscribeFunctionTests` | 2 stubs, 4 verifications | not used | — |
| `SendEmailFunctionTests` | not used | not used | `IEmailSender`, 1 verification |

16 + 9 + 1 = the 26 verification sites quoted above. Stub counts are statements, not tests — the sixth
Subscribe test stubs `FindByEmailAsync` twice to walk two scenarios in one test.

**Three of those verifications cannot be expressed by a fake at all.** They assert the store was never *asked*,
not that its state is unchanged — which is a statement about a call, not about state:

- `SubscribeFunctionTests.cs:94` — a filled honeypot must not even look the email up
- `WeeklyDigestFunctionTests.cs:60` and `:73` — no posts in the window means never enumerate subscribers, so a
  quiet week costs no Table Storage scan

A dictionary-backed fake can only carry those by exposing `FindByEmailCalled` / `ListByStatusCalled` flags,
which is a mock wearing a fake's clothes. So "replace the mocks with fakes" was never actually available for
the two files with the most verifications; the honest choice was a fake in `Confirm`/`Unsubscribe` only, for a
saving of 8 verification sites across two four-test files. Not worth a second test double idiom in one project.

Revisit if either becomes true: a test needs to arrange several Subscribers and assert over the resulting set
(the point where `Returns` chains stop reading as behaviour), or the mocks start encoding *sequences* of store
calls rather than single ones.

### `SendEmailFunctionTests`: kept, renamed.

Decided by Lee, 2026-08-26. The test stays as a wiring regression guard — proof the queue-triggered function
reaches the sender at all — because delegation is the only observable effect through this seam. The name was
the actual defect: `HandleAsync_SendsTheQueuedMessageViaTheEmailSender` names the collaborator, so it describes
the mechanism. Renamed to `HandleAsync_ForAQueuedMessage_SendsTheEmailToItsRecipient`, matching the
`HandleAsync_<Given>_<Then>` shape its four sibling files already use.

The `Received(1)` assertion is unchanged and is not a red flag here for the same reason `mocking.md` gives:
`IEmailSender` is a port over Resend, and a real call leaves the process.
