# 01 — Name the reader's local subscription state

Type: grilling
Status: resolved
Blocked by: —

## Question

The backend has a **Subscriber** — a server-side record with `Pending`/`Active`/`Unsubscribed`.
The widget needs a *second*, browser-local notion of the same person that is deliberately not
the same thing: it lives in one browser's `localStorage`, it is a guess rather than a fact, and
it carries a state the Subscriber has no equivalent for (`dismissed`).

Naming these two apart is the whole point of this ticket — if the local thing is also called a
"subscriber", every later conversation about the widget is ambiguous.

Settle:

- What the browser-local concept is **called**, and what `CONTEXT.md` entry defines it against
  **Subscriber** (with an `_Avoid_:` line, matching the existing entries' shape).
- What its four states are called, given `pending`/`subscribed`/`unsubscribed`/`dismissed` are
  placeholders chosen at charting, not decided names.
- The `localStorage` key, the stored shape, and how a schema change is handled — a version
  field, a versioned key, or simply treating anything unparseable as "no record".
- How long a dismissal lasts before the widget may ask again, and whether that duration is a
  build-time constant or something the embedding page can set.
- What happens on the two known edge cases: `sixsideddice.com` and `www.sixsideddice.com` are
  separate origins so state does not carry between them, and state is per-browser so confirming
  on a phone leaves a laptop still prompting. Decide whether each is accepted, mitigated, or
  designed around.

Consult `/domain-modeling` — the outcome includes a real `CONTEXT.md` edit, not just a note here.

## Answer

### The name

**Signup Record**, defined in `CONTEXT.md` against **Subscriber**:

> **Signup Record**:
> A browser-local note of how a reader last dealt with the signup prompt in this browser —
> `submitted`, `confirmed`, `optedOut` or `dismissed`. It is a guess about the Subscriber, never
> a fact: it may say `confirmed` where no Subscriber exists, and an Active Subscriber may have no
> Signup Record at all.
> _Avoid_: local subscriber, subscription state, subscriber cache

The key/shape/durations below are deliberately kept out of `CONTEXT.md` — that file is a glossary,
and these are spec detail.

### The four states

Named for what the reader **did in this browser**, so no word is shared with the Subscriber's
Pending/Active/Unsubscribed and the two models cannot be confused at a glance:

| Signup Record state | What this browser observed | Likely Subscriber state — never assumed |
|---|---|---|
| `submitted` | The form was submitted here | Pending |
| `confirmed` | The confirm page was reached here, or the reader used the "I'm already subscribed" control | Active |
| `optedOut` | The unsubscribe page was reached here | Unsubscribed |
| `dismissed` | The prompt was closed without signing up | none — the server never hears about this |

Absence of a record means "not asked yet". There is no stored `none`.

### Key, shape and schema change

- **Key**: `six-sided.signup` — namespaced to match the `six-sided-*` element prefix, because the
  origin serves the whole blog.
- **Value**: `{ "v": 1, "state": "confirmed", "at": "2026-08-26T17:51:00.000Z" }`. `at` is when the
  record was last written and exists solely to age records out.
- **The email address is never stored.** Prefilling a returning reader's form is not worth putting
  an address in plain `localStorage` on a shared or family browser, readable by any script on the origin.
- **Schema change**: keep `v`, write no migration code. Anything not fully recognised — unparseable
  JSON, unknown `v`, a `state` outside the four, a malformed `at` — is treated as **no record**. The
  blast radius is one extra prompt, and resubmitting is idempotent server-side. A versioned key
  (`six-sided.signup.v1`) was rejected: superseded keys linger on readers' machines with nothing to
  clean them up.
- **Blocked storage**: `localStorage` throws rather than returning null when a browser blocks site
  data (Safari private mode, hardened settings). Every read and write is wrapped — a failed read is
  "no record", a failed write is a silent no-op — so such a browser behaves like a first visit every
  time, which is the correct degradation.

### Ageing

Evaluated **on read**; an expired record is reported as "no record" and the next write overwrites it.
No proactive cleanup, no timers. All three durations are **build-time constants in this repo**, not
settable by the embedding page: a `data-` attribute would put the value in the BlogToHtml template in
`C:/Dev/Personal/Blog`, where changing it means a template edit plus a full blog regeneration, and it
would break the charting decision that the blog contract is exactly one `<script type="module">` line.

| State | Ages out after | Why |
|---|---|---|
| `dismissed` | 30 days | A soft "not now"; a month later on a different article is fair. 7 days nags a regular reader |
| `submitted` | 7 days | Long enough for a "I'll do it at the weekend" reader; past that the confirm email is lost and re-prompting is the only recovery |
| `confirmed` | never | Already in; asking again is pure noise |
| `optedOut` | never | Permanent, per the charting decision. Re-asking someone who opted out is the one genuinely rude outcome |

`submitted` was not in the original list of things to age. It needs an expiry because a reader who
submits and never clicks the confirm email would otherwise hold a `submitted` record forever, never be
prompted again, and never become a Subscriber — a silently lost reader, and the only failure here that
nobody ever finds out about.

### Edge cases

**Apex vs `www` are separate origins — accepted, and effectively moot.** Checked rather than assumed:
`http://sixsideddice.com/` returns **301** to `http://www.sixsideddice.com/` (the apex is Google
domain-forwarding on `216.239.32-38.21`, not GitHub Pages), `www` is the CNAME target and the canonical
host in every `<link rel="canonical">`, and `https://sixsideddice.com/` fails TLS outright while
`https://www.sixsideddice.com/` returns 200. Readers can only land on `www`. The spec notes in one line
that if the apex ever serves the site directly, records do not carry across and the reader sees one
extra prompt.

**State is per-browser — accepted, with one mitigation.** Designing around it needs a server-side
"am I subscribed?" endpoint, already ruled out on the map as an email-enumeration oracle. The mitigation
is an **"I'm already subscribed" control** on the widget that writes a `confirmed` Signup Record locally
and never calls the server — it asserts nothing and learns nothing, so it carries no enumeration risk,
and it turns a recurring nag into a one-click permanent fix. Its visibility and wording belong to
[Prototype the article widget](02-prototype-the-article-widget.md); this ticket establishes only that
the state may be set that way.

### The tension to carry forward, not considered closed

The 7-day `submitted` expiry is simultaneously the rescue for a lost confirmation email and the cause of
cross-device nagging — one reader confirms on a phone, their laptop re-prompts a week later. Same
mechanism, both outcomes. Chosen deliberately, because a lost reader is silent and a redundant form fill
is not; the server treats a resubmission from an Active address as a no-op.

### Open rider

"The widget guesses locally rather than asking the server" arguably clears the ADR bar — hard to reverse,
surprising to a future reader, and a real privacy-versus-correctness trade-off. Whether it becomes an ADR
or is carried by the spec plus the map's Out-of-scope line is **not yet decided**; it belongs to
[Write the spec and implementation tickets](08-write-the-spec-and-tickets.md) either way.
