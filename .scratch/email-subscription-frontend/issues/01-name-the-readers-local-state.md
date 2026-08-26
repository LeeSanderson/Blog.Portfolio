# 01 — Name the reader's local subscription state

Type: grilling
Status: open
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
