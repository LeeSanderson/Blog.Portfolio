# 04 — Extract email builders and cover them with HTML snapshot tests

**What to build:** Email rendering moves out of the two functions that send email into one builder per email,
each covered by a snapshot test whose approved file is real, browser-openable HTML committed to the repo. No
email content changes — the markup moves verbatim, and the approved files document today's output exactly so
the extraction is provably faithful.

**Blocked by:** nothing (01–03 are done)

**Status:** ready-for-agent

See `spec-email-snapshot-testing.md` for the full reasoning.

- [ ] `ConfirmationEmailBuilder` and `DigestEmailBuilder` added under the app's `Services/Email` area — one
      class per email, concrete with no interface, each taking `SubscriberLinkBuilder` and the app's options
      and exposing `Build` returning `SendEmailMessage` (subject and HTML body both from the builder)
- [ ] Both registered as singletons alongside `SubscriberLinkBuilder`, matching that existing precedent
- [ ] `DigestEmailBuilder`, not `WeeklyDigestEmailBuilder` — the 7-day window and Monday 08:00 UTC schedule
      stay in the timer function, which is what the name has to reflect
- [ ] HTML moves **verbatim** from both functions; no wording, markup, whitespace or subject-line changes
- [ ] `SubscribeFunction` drops the link builder and options object (4 constructor dependencies → 3)
- [ ] `WeeklyDigestFunction` drops the same two and gains the digest builder (5 → 4); post filtering, the
      empty-window early return, and one-message-per-Active-Subscriber logic all stay put
- [ ] `Verify.Xunit` (xunit v2 line — this solution is on xunit 2.x) and `Verify.DiffPlex` added to the
      solution's central package versions and referenced by the app's test project
- [ ] Module initializer in the test project disables the diff-tool launcher and enables DiffPlex, so a
      failure prints an inline textual diff and no run ever opens a GUI window — agents run this suite
- [ ] `*.received.*` added to `.gitignore`
- [ ] New `.gitattributes` pins `*.verified.html` to LF, so approved files aren't rewritten per-platform
- [ ] One snapshot for the confirmation email (the email has no variants — new, reopened and resent-to-Pending
      all render identically)
- [ ] One snapshot for the Digest with two posts, pinning list repetition and order. Fixture is invented
      plain-ASCII content with no apostrophes or other characters ticket 05's encoding will touch, so both
      approved files stay byte-identical across that ticket
- [ ] Each snapshot verifies the HTML body alone to an approved `.html` file; recipient and subject asserted
      in the same test with FluentAssertions
- [ ] Tests use a hard-coded Subscriber id and the real HMAC token service with the signing key the existing
      tests already use, so approved files contain genuine signed confirm/unsubscribe URLs
- [ ] Test files mirror the source layout, with approved files beside them (Verify's default — no directory
      configuration)
- [ ] The two existing function test classes construct the real concrete builder; **no assertion in them
      changes** — that unchanged suite is the evidence this ticket preserved behaviour
- [ ] `CONTEXT.md` gains a **Digest** entry recording that the email and its weekly cadence are separate
      concerns, so `DigestEmailBuilder` and `WeeklyDigestFunction` don't read as two different things
- [ ] `CODING_STANDARDS.md` testing-stack line extended with Verify, the accept workflow, and why the diff
      launcher is off (that line names only xUnit/FluentAssertions/Moq/coverlet today and becomes wrong the
      moment Verify lands)
- [ ] ADR-0009 records choosing Verify over a hand-rolled approved-file comparison
- [ ] Suite green on Windows **and** on the Linux CI runner

**Verify before assuming — each has a decided fallback:**

- [ ] Verify's licence terms — check before the package reference lands
- [ ] Verify's line-ending normalization actually holds — prove it against both a CRLF and an LF approved
      file. If it doesn't, make the builders' output independent of source-file line endings (this repo has
      `core.autocrlf=true`, so the working tree is CRLF while CI checks out LF, and the HTML comes from
      multi-line raw string literals)
- [ ] The current Verify major still supports xunit v2 — if it has been dropped, **stop and raise the xunit
      v3 upgrade with Lee**; do not perform a solution-wide migration inside this ticket
