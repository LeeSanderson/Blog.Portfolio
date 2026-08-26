# 03 — Consider replacing the mocked stores with in-memory fakes

**What to decide:** Whether the five function tests should mock `ISubscriberStore` and `IEmailOutbox` at all,
or whether hand-written in-memory fakes would serve better — and separately, whether
`SendEmailFunctionTests` should exist in its current form.

**Blocked by:** 02 — there is no point rethinking the mocks until they have stopped moving.

**Status:** needs-triage

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
