# 05 — HTML-encode every RSS field the Digest renders

**What to build:** The Digest currently interpolates the RSS title, description and link into HTML raw, so a
post titled `Dice & Dragons` emits an unescaped ampersand and angle brackets in a title would inject markup.
Encode all three, uniformly, driven by its own failing test.

**Blocked by:** 04 (the snapshot safety net has to exist first, so this change is provably the only behaviour
change and isn't bundled into the extraction)

**Status:** done

See `spec-email-snapshot-testing.md` for the full reasoning.

- [x] A focused FluentAssertions test on an awkward post title, written **red first** — it must fail against
      the unencoded builder before the fix lands
- [x] All three interpolated RSS values HTML-encoded in `DigestEmailBuilder`: title, description, and the post
      link where it is used as an `href`
- [x] One uniform rule, no per-field exceptions — nothing that needs an explanatory comment to be
      understandable
- [x] The confirmation email is untouched: it renders no feed content, only generated links
- [x] Both approved `.html` files unchanged by this ticket. Ticket 04's fixture is deliberately plain ASCII, so
      any churn here means the fixture drifted — investigate rather than re-accept
- [x] Encoding kept out of the snapshots so "template shape" and "encoding rule" fail independently, each
      failure naming its own cause
- [~] Suite green on Windows (verified locally). Linux CI not yet observed — see ticket 04's note.

**Context that shaped this:**

- An earlier proposal encoded only the title and `href`, leaving the description raw as deliberate markup
  pass-through. Withdrawn on evidence: the live feed's descriptions are plain text with no markup, no CDATA
  and no entities, so the exception was justifying itself on something untrue of this feed.
- Accepted consequence: apostrophes in feed text render as character references in the approved file —
  visually identical in a mail client, marginally noisier in a diff.
- If a future description ever does carry real markup, the Digest snapshot fails and the decision gets
  revisited with the evidence in hand. That failure is the intended signal, not a nuisance to accept away.
- Found by reading the code during a grilling session, not from a reported bug — no Subscriber is known to
  have received a mangled Digest. It is nonetheless live in production.

## Comments

Written red first. Against the unencoded builder the test failed with the live bug visible in the message:
`<li><a href="...?tag=d&d">Dice & Dragons: <b>a primer</b></a>...` — the `<b>` tags landing as markup rather
than as text, which is the injection the ticket describes.

The fix routes all three feed values through `HtmlEncoder.Default.Encode` in a `RenderPost` helper, so every
field the Digest takes from the feed visibly goes through the same call and there is no per-field exception
to remember.

Both approved `.html` files came through unchanged — `git diff -- '*.verified.html'` is empty — confirming
ticket 04's fixture stayed clear of the characters this encoding touches.
