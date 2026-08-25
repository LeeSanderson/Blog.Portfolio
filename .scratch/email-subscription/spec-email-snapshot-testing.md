# EmailSubscription — Extracted Email Builders with HTML Snapshot Tests

Status: ready-for-agent

## Problem Statement

Lee has no way to see, review, or safely change the HTML of the emails this app sends to readers of
sixsideddice.com.

Both emails — the confirmation email and the Digest — are assembled as interpolated string literals inside the
very functions that send them, and every existing test asserts only *who* an email went to, never *what it
said*. The consequences compound:

- The entire body of every email the blog sends is untested. Subject lines are untested too.
- There is no artifact anywhere in the repo that shows what a Subscriber actually receives. To see the
  confirmation email, Lee has to subscribe himself and read his own inbox.
- Any edit to the markup — adding branding, fixing a broken link, restructuring the post list — is
  unverifiable short of that same manual round trip, so small presentational improvements carry
  disproportionate risk and don't get made.
- A rendering bug can ship to every Active Subscriber with the whole suite green. One is live today: the
  Digest interpolates the RSS `title`, `description` and `link` raw, so a post titled `Dice & Dragons`
  emits an unescaped ampersand, and angle brackets in a title would inject markup.
- Because rendering is entangled with orchestration, the two functions each carry the link builder and the
  options object purely to build strings, obscuring what the functions are actually responsible for.

## Solution

Pull email rendering out of the functions into two small, directly-testable builders — one per email — and
cover each with a snapshot test whose approved file is real, browser-openable HTML committed to the repo.

After this change, reviewing an email change means reading an HTML diff in the pull request, and seeing what a
Subscriber receives means double-clicking a file. With that safety net in place, fix the unescaped
interpolation the snapshots expose, as a separate change whose effect is visible in its own right.

Delivered as two sequential tickets so the refactor and the behaviour change never mask each other:

- **Ticket 04** extracts the builders and lands the snapshots against today's exact output, proving the
  extraction changed nothing.
- **Ticket 05** applies uniform HTML encoding, driven by its own failing test.

## User Stories

1. As Lee, I want to open a file in the repo and see exactly what the confirmation email looks like, so that
   I can judge its wording and layout without subscribing myself and waiting for an inbox.
2. As Lee, I want to open a file in the repo and see exactly what the Digest looks like, so that I can judge
   how a week's posts are presented without waiting for a Monday.
3. As Lee, I want a change to email markup to show up as a readable HTML diff in review, so that I can see
   what a Subscriber's experience will become rather than inferring it from a changed string literal.
4. As Lee, I want the test suite to fail when email output changes unintentionally, so that a refactor
   elsewhere can't silently alter what readers receive.
5. As Lee, I want an intentional email change to be accepted deliberately, so that updating the approved
   output is a conscious act I can see in the diff rather than something that happens by accident.
6. As Lee, I want email rendering to live in its own unit with its own tests, so that I can change wording or
   layout without touching, re-reading, or re-testing the orchestration around it.
7. As Lee, I want the functions to stop carrying dependencies they only need for string building, so that
   each function's constructor tells me what that function is genuinely responsible for.
8. As Lee, I want the confirmation email's rendering testable without going through the subscribe flow, so
   that a rendering test doesn't need a subscriber store, an outbox, or a state-transition arrangement.
9. As Lee, I want email snapshots to be deterministic, so that a test that passes on my machine passes on CI
   and a failure always means something genuinely changed.
10. As Lee, I want the approved files to contain genuine signed confirm and unsubscribe URLs, so that the
    artifact is a real email I can click through rather than a redacted approximation.
11. As Lee, I want the same suite to be green on Windows and on the Linux CI runner, so that line-ending
    differences between my working tree and CI never produce a phantom failure.
12. As an agent working in this repo, I want a failing snapshot to print a textual diff in the test output,
    so that I can see what changed and act on it from the console alone.
13. As an agent working in this repo, I want running the test suite never to open a GUI window, so that an
    unattended run can't block on something no agent can see or dismiss.
14. As a Subscriber, I want post titles containing an ampersand to display correctly in the Digest, so that a
    normal English title doesn't arrive looking broken.
15. As a Subscriber, I want a post title containing angle brackets to appear as text rather than being
    interpreted as markup, so that the email I receive can't be structurally mangled by feed content.
16. As a Subscriber, I want the link on each post in the Digest to work regardless of the characters in its
    URL, so that a query string can't break the surrounding anchor tag.
17. As a Subscriber, I want every Digest to keep carrying a working personalized unsubscribe link, so that
    this refactoring never costs me the ability to opt out.
18. As Lee, I want the encoding fix driven by its own failing test, so that the fix is demonstrably a fix
    rather than a change I assert is one.
19. As Lee, I want the encoding fix to leave the approved files untouched, so that reviewing it means
    reviewing one focused test rather than auditing two HTML diffs for unrelated churn.
20. As a reviewer of ticket 04, I want the approved output to match today's behaviour byte for byte, so that
    I can confirm the extraction was faithful without reading both versions of the markup side by side.
21. As Lee, I want a single documented rule for how feed content is encoded, so that a future email doesn't
    have to guess which fields are trusted.
22. As Lee, I want the builders registered and injected the same way as the existing link builder, so that
    the app has one pattern for this kind of collaborator rather than two.
23. As Lee, I want the tests to sit where this repo's conventions say they sit, so that the layout stays
    predictable as more emails are added.
24. As Lee, I want adding a third email later to mean adding a class and a snapshot, so that email templates
    scale without one type accreting a method per email.
25. As Lee, I want the snapshot tooling recorded in the coding standards, so that the next contributor knows
    the accept workflow exists and doesn't hand-roll a second approach.
26. As Lee, I want the Digest term pinned in the glossary, so that a reader understands the email and its
    weekly cadence are separate concerns and doesn't assume the two are one thing.
27. As Lee, I want working files produced by a failed snapshot kept out of version control, so that a local
    failure never becomes an accidental commit.

## Implementation Decisions

### Email rendering moves into one builder per email

- Two new types in the app's `Services/Email` area: `ConfirmationEmailBuilder` and `DigestEmailBuilder`. Each
  takes the existing `SubscriberLinkBuilder` and the app's options, and exposes a `Build` method returning the
  existing `SendEmailMessage` record — subject and HTML body both come from the builder.
- One class per email, not one class with a method per email. Each function then depends only on the email it
  actually sends, and a third email is a new class rather than a growing one.
- Both are concrete classes with no interface, registered as singletons alongside `SubscriberLinkBuilder`.
  This follows the precedent already set by `SubscriberLinkBuilder` and `SubscriberLinkAction`, which are
  injected as concrete types; adding single-implementation interfaces purely to enable mocking is rejected.
- They live under `Services/Email` rather than beside the functions that call them, per ADR-0008: a
  `Services` area is named for its responsibility, not for whichever function happens to call it. The
  confirmation email is not a Subscribe concern just because Subscribe sends it.
- `Build` mirrors the naming and shape of `SubscriberLinkBuilder.Build`.
- The digest builder receives the already-filtered posts. It renders whatever set it is given.

### The builders are named for what they render, not when they run

- `DigestEmailBuilder`, not `WeeklyDigestEmailBuilder`. The seven-day window and the Monday 08:00 UTC
  schedule are the timer function's concern; the builder knows nothing about cadence and would be misnamed
  the day a manual or differently-scheduled send exists.
- Because the surrounding spec, README and function name all say "weekly digest", **Digest** gains a
  `CONTEXT.md` glossary entry recording that split, so the two names don't read as two concepts.

### The functions keep orchestration and lose rendering

- `SubscribeFunction` drops the link builder and the options object, going from four constructor
  dependencies to three; its private email-sending helper becomes a call to the confirmation builder followed
  by the existing enqueue.
- `WeeklyDigestFunction` likewise goes from five to four, keeping the feed reader, the subscriber store, the
  outbox and the digest builder. Post filtering, the empty-window early return, and one-message-per-Active-
  Subscriber all stay exactly where they are.
- Neither function's behaviour changes in ticket 04. The HTML moves verbatim.

### Snapshot comparison uses Verify

- `Verify.Xunit` (this solution is on xunit 2.x, so the xunit v2 package, not the v3 one) plus
  `Verify.DiffPlex`, added centrally to the solution's package versions and referenced by the app's test
  project. Both are new dependencies.
- A hand-rolled approved-file comparison was rejected: it means owning the accept workflow, the
  received-file lifecycle, and a guard against silent auto-approval on CI, for no benefit over a maintained
  library.
- The diff-tool launcher is disabled in the test project via a module initializer, and DiffPlex is enabled so
  a failure carries an inline textual diff in the test output. This is decided specifically because agents
  run the suite on this machine: an agent can read console output but cannot read or dismiss a diff window.
- Working files from a failed comparison are git-ignored.

### The approved file is browser-openable HTML

- Each snapshot verifies the HTML body alone, to an approved `.html` file, so the artifact is a real email
  that renders when opened. Serializing the whole `SendEmailMessage` into one approved file is rejected: it
  escapes the HTML into a blob that neither renders nor diffs well.
- Recipient and subject are asserted in the same test with ordinary FluentAssertions, where a failure names
  the mismatched value instead of pointing at a file diff.
- Approved files sit beside their test file — Verify's default, needing no directory configuration, and
  consistent with this repo's rule that tests mirror the source layout.

### Snapshots are deterministic by construction

- Tests pass a hard-coded Subscriber id and use the real HMAC token service with the signing key the existing
  tests already use, so the approved files contain genuine signed confirm and unsubscribe URLs. Scrubbers and
  stub token services are both rejected as unnecessary machinery given the inputs are already controllable.
- Testing the builders directly removes the non-determinism that would otherwise block this: the id is
  supplied by the test rather than generated inside `SubscribeFunction`.
- The Digest fixture is two invented plain-ASCII posts with no characters that encoding will touch — so the
  approved files are byte-identical before and after ticket 05, and ticket 05's diff is one focused test.

### Line endings are pinned so CI and Windows agree

- The HTML comes from multi-line raw string literals, whose newlines are whatever the source file holds on
  disk. This repo has `core.autocrlf=true` and no `.gitattributes`, so the working tree holds CRLF while the
  Linux CI runner checks out LF — a real cross-platform hazard for any snapshot containing newlines.
- Mitigation: a new `.gitattributes` pins approved HTML files to LF so they are never rewritten per-platform,
  relying on Verify's line-ending normalization for the comparison itself. That normalization is to be proven
  empirically during ticket 04, not assumed.
- Fallback if it doesn't hold: make the builders' output independent of source-file line endings. Normalizing
  inside each test is rejected — it is per-test ceremony that hides a genuine difference in what production
  emits.

### Feed content is uniformly encoded (ticket 05)

- All three interpolated RSS values — title, description, and the post link used as an `href` — are HTML
  encoded. One rule, no exceptions to remember, and nothing needing an explanatory comment.
- An earlier proposal to leave the description raw as deliberate markup pass-through was withdrawn on
  evidence: the live feed's descriptions are plain text with no markup, no CDATA and no entities, so the
  exception was justifying itself on something untrue of this feed. If a description ever does carry markup,
  the Digest snapshot fails and the decision gets revisited with evidence in hand.
- Consequence accepted: apostrophes in feed text appear as character references in the approved file.
  Visually identical in a mail client, marginally noisier in a diff.
- The confirmation email renders no feed content — only generated links — so ticket 05 does not touch it.

### Decisions recorded

- An ADR records choosing Verify over a hand-rolled comparison for HTML email snapshots. Noted as a
  deliberate departure from the usual bar for an ADR in this repo: the choice is cheaply reversible, so it
  would ordinarily be skipped, but Lee wants the tool choice defended in writing.
- The coding standards' testing-stack line currently names only xUnit, FluentAssertions, Moq and coverlet; it
  gains Verify, the accept workflow, and the disabled-launcher rationale. Updated in ticket 04, alongside the
  change that makes it true.

## Testing Decisions

### What makes a good test here

A good test in this area asserts what a Subscriber would receive — subject line, recipient, and rendered HTML
— and nothing about how that output was assembled. It names no private helper, asserts no intermediate
string, and counts no method calls. A snapshot test earns its place precisely because it commits to the
observable artifact: the file is the email.

The corollary is that an approved file must never be re-accepted to make a build green. A snapshot diff is a
question about behaviour, and blind acceptance converts the suite from a safety net into a rubber stamp.

### The seam

One new seam: the builders' `Build` method. Two classes, a single seam shape, called directly with plain
values — no mocks, no orchestration arrangement.

The higher existing seam was considered and rejected. Driving `Endpoint.HandleAsync` (or the digest function's
`HandleAsync`) and capturing the message at the existing `IEmailOutbox` mock adds no seam at all, and is
genuinely the higher point. It loses on three counts: the confirmation email is not deterministic through it,
because `SubscribeFunction` generates the Subscriber id internally; every additional snapshot case would need
a full orchestration arrangement to reach the rendering; and the resulting test couples email content to
subscriber state transitions that have nothing to do with markup. The builder seam is one level lower and buys
determinism, isolation, and a genuinely reviewable artifact.

No other seam changes. The existing function seams remain exactly as they are.

### What is tested

- `ConfirmationEmailBuilder` — one snapshot. The email has no variants: a new, reopened and re-sent-to-Pending
  Subscriber all produce identical content, so a second case would duplicate the first.
- `DigestEmailBuilder` — one snapshot, with two posts, pinning list repetition and ordering. A single-post
  case is omitted deliberately: the post list is concatenated with no separator, so one post renders as a
  strictly shorter version of two and the case would add an approved file without adding coverage.
- Encoding (ticket 05) — one focused FluentAssertions test on an awkward title, kept out of the snapshots so
  that "template shape" and "encoding rule" fail independently and each failure names its own cause. Written
  red first: it must fail against the unencoded builder before the fix lands.
- The zero-new-posts case gets no snapshot, because the digest function returns before reaching the builder;
  that path is already covered at the function seam.

### Existing tests that change

- The two function test classes construct the real concrete builder instead of the link builder and options
  object, and continue asserting recipient and call counts only. Their arrangements get shorter. No assertion
  in them changes, which is itself the evidence that ticket 04 preserved behaviour.

### Prior art

- The function tests already establish the pattern of exercising a handler seam directly with the real
  `SubscriberLinkBuilder` over the real HMAC token service with a fixed test signing key — the snapshot tests
  use the same collaborators the same way.
- The token service tests are the reference for a service tested directly with fixed inputs, and remain the
  single place the signature format itself is pinned.
- The layout convention is the existing tests mirroring the source structure.

## Out of Scope

- Any change to how email is delivered — the outbox queue, the queue-triggered send function, and the Resend
  sender are all untouched. ADR-0006 stands.
- Any change to the token scheme, link format, or signature algorithm. ADR-0007 stands.
- Plain-text email bodies, MIME multipart, or anything beyond the existing HTML-only message shape.
- Email styling, branding, or a shared layout wrapper. A layout-plus-content structure was considered and
  rejected as unjustified by two emails; the per-email builders leave the door open.
- Rendering-fidelity testing across real mail clients — the snapshot pins the HTML this app emits, not how
  Gmail or Outlook chooses to display it.
- HTML validity or accessibility linting of the output.
- A third email of any kind, and any change to which events send email.
- Migrating the solution to xunit v3, unless the chosen Verify package turns out to require it, in which case
  it comes back to Lee as a decision rather than being done silently.
- Converting existing source files' line endings, or adding blanket `.gitattributes` rules beyond the
  approved-file pattern.
- Any change to the subscribe, confirm or unsubscribe endpoint behaviour, the state machine, or the honeypot.

## Further Notes

- Produced via a `/grilling` session followed by `/to-spec`.
- This app's original `spec.md` is `Status: done` and describes the initial build; this document is follow-up
  work on the same app, which is why it sits beside that file and why its tickets continue the existing
  numbering at 04 rather than opening a new feature directory.
- Three facts are to be verified during ticket 04 rather than assumed, each with a decided fallback:
  1. **Verify's licence terms** — check before the package reference lands.
  2. **Verify's line-ending normalization** — prove it against both a CRLF and an LF approved file. If it
     does not hold, make the builder output independent of source-file line endings.
  3. **xunit v2 support in the current Verify major** — if it has been dropped, stop and raise the xunit v3
     upgrade with Lee rather than performing a solution-wide migration inside this ticket.
- The unescaped interpolation was found by reading the code during the grilling session, not by a reported
  bug — no Subscriber is known to have received a mangled Digest. It is nonetheless live, and the two-ticket
  split exists so the fix is provable rather than bundled into a refactor.
