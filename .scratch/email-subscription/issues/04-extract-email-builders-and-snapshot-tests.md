# 04 — Extract email builders and cover them with HTML snapshot tests

**What to build:** Email rendering moves out of the two functions that send email into one builder per email,
each covered by a snapshot test whose approved file is real, browser-openable HTML committed to the repo. No
email content changes — the markup moves verbatim, and the approved files document today's output exactly so
the extraction is provably faithful.

**Blocked by:** nothing (01–03 are done)

**Status:** done

See `spec-email-snapshot-testing.md` for the full reasoning.

- [x] `ConfirmationEmailBuilder` and `DigestEmailBuilder` added under the app's `Services/Email` area — one
      class per email, concrete with no interface, each taking `SubscriberLinkBuilder` and the app's options
      and exposing `Build` returning `SendEmailMessage` (subject and HTML body both from the builder)
- [x] Both registered as singletons alongside `SubscriberLinkBuilder`, matching that existing precedent
- [x] `DigestEmailBuilder`, not `WeeklyDigestEmailBuilder` — the 7-day window and Monday 08:00 UTC schedule
      stay in the timer function, which is what the name has to reflect
- [x] HTML moves **verbatim** from both functions; no wording, markup, whitespace or subject-line changes
- [x] `SubscribeFunction` drops the link builder and options object (4 constructor dependencies → 3)
- [x] `WeeklyDigestFunction` drops the same two and gains the digest builder (5 → 4); post filtering, the
      empty-window early return, and one-message-per-Active-Subscriber logic all stay put
- [x] `Verify.Xunit` (xunit v2 line — this solution is on xunit 2.x) and `Verify.DiffPlex` added to the
      solution's central package versions and referenced by the app's test project
- [x] Module initializer in the test project disables the diff-tool launcher and enables DiffPlex, so a
      failure prints an inline textual diff and no run ever opens a GUI window — agents run this suite
- [x] `*.received.*` added to `.gitignore`
- [x] New `.gitattributes` pins `*.verified.html` to LF, so approved files aren't rewritten per-platform
- [x] One snapshot for the confirmation email (the email has no variants — new, reopened and resent-to-Pending
      all render identically)
- [x] One snapshot for the Digest with two posts, pinning list repetition and order. Fixture is invented
      plain-ASCII content with no apostrophes or other characters ticket 05's encoding will touch, so both
      approved files stay byte-identical across that ticket
- [x] Each snapshot verifies the HTML body alone to an approved `.html` file; recipient and subject asserted
      in the same test with FluentAssertions
- [x] Tests use a hard-coded Subscriber id and the real HMAC token service with the signing key the existing
      tests already use, so approved files contain genuine signed confirm/unsubscribe URLs
- [x] Test files mirror the source layout, with approved files beside them (Verify's default — no directory
      configuration)
- [x] The two existing function test classes construct the real concrete builder; **no assertion in them
      changes** — that unchanged suite is the evidence this ticket preserved behaviour
- [x] `CONTEXT.md` gains a **Digest** entry recording that the email and its weekly cadence are separate
      concerns, so `DigestEmailBuilder` and `WeeklyDigestFunction` don't read as two different things
- [x] `CODING_STANDARDS.md` testing-stack line extended with Verify, the accept workflow, and why the diff
      launcher is off (that line names only xUnit/FluentAssertions/Moq/coverlet today and becomes wrong the
      moment Verify lands)
- [x] ADR-0009 records choosing Verify over a hand-rolled approved-file comparison
- [~] Suite green on Windows (verified: 25/25 locally). The Linux CI runner half is **not yet observed** —
      both commits are unpushed, so no CI run exists. Reopen this box if Backend CI goes red on push.

**Verify before assuming — each has a decided fallback:**

- [x] Verify's licence terms — check before the package reference lands
- [x] Verify's line-ending normalization actually holds — prove it against both a CRLF and an LF approved
      file. If it doesn't, make the builders' output independent of source-file line endings (this repo has
      `core.autocrlf=true`, so the working tree is CRLF while CI checks out LF, and the HTML comes from
      multi-line raw string literals)
- [x] The current Verify major still supports xunit v2 — if it has been dropped, **stop and raise the xunit
      v3 upgrade with Lee**; do not perform a solution-wide migration inside this ticket

## Comments

**Verification results** (the three facts this ticket said to check rather than assume):

1. **Licence** — `Verify.Xunit` 31.12.5 and `Verify.DiffPlex` 3.1.2 are both MIT. Clear.
2. **Line-ending normalization** — holds, proven in both directions. A CRLF approved file passes against
   LF output, and an LF approved file passes against CRLF output (produced by converting the builders'
   source files to CRLF, i.e. simulating a fresh clone under `core.autocrlf=true`). The `.gitattributes`
   mitigation stands; the "make builder output independent of source line endings" fallback is not needed.
3. **xunit v2 support** — *deprecated, not dropped.* `Verify.Xunit` was marked "Legacy" on 2026-02-12 with
   `Verify.XunitV3` as its alternate, and is frozen at 31.12.5 while Verify's core package has moved on to
   31.28.0. It still works and needed no v3 migration, so this ticket proceeded rather than stopping — but
   it is a dead-end package, so **the xunit v3 upgrade is now a real decision for Lee**, raised here rather
   than performed silently. Recorded in ADR-0009.

Two consequences of that third point:

- `Verify.DiffPlex` is held at **3.1.2**, not the current 3.3.1. Later releases require Verify core 31.24.0+,
  which is binary incompatible with the frozen `Verify.Xunit` and fails at runtime with a
  `MethodAccessException` rather than a build error.
- `SolutionDir`/`SolutionName` are now set in `Directory.Build.props`. Verify auto-discovers the solution by
  searching only three directory levels up; test projects here sit four below the root, so discovery failed
  the build until it was told explicitly.

**Faithfulness evidence:** before extracting anything, both emails were rendered through the *old* function
code paths and captured to disk. The snapshots Verify then produced from the new builders were byte-identical
to that capture — the only difference being the UTF-8 BOM Verify writes into the file itself. The approved
files therefore document today's output exactly, and no assertion in either function test class changed.

**Post-review corrections** (from `/code-review`, two axes):

- The "Suite green on Windows **and** on the Linux CI runner" box was ticked on the Windows half only — the
  commits are unpushed, so no CI run exists. Downgraded to `[~]` above rather than left claiming something
  unobserved.
- The xunit v3 question is now tracked as its own decision ticket, `06-decide-on-xunit-v3-migration.md`,
  rather than living only in an ADR paragraph. The DiffPlex pin is that guard's symptom already being paid.
- User story 21 ("a single documented rule for how feed content is encoded") was not actually delivered by
  ticket 05 — the rule existed only as the shape of `RenderPost`. Now written down in `CODING_STANDARDS.md`.
- `Directory.Build.props` gains repo-wide `SolutionDir`/`SolutionName`. Flagged for Lee as the one piece of
  this change that reaches outside the app: Verify's build-time solution discovery searches three directory
  levels up, and test projects here sit four below the root, so the build failed until it was told
  explicitly. It is condition-guarded (`'$(SolutionDir)' == ''`), so it yields to any real solution build.
  Unrelated to Verify's *approved-file* directory, which is left at its default as the ticket asked.
