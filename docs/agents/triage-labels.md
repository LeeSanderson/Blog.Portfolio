# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker, plus one label this repo adds that the skills have no role for.

| Label in mattpocock/skills | Label in our tracker | Meaning                                   |
| --------------------------- | --------------------- | ----------------------------------------- |
| `needs-triage`               | `needs-triage`        | Maintainer needs to evaluate this issue   |
| `needs-info`                 | `needs-info`          | Waiting on reporter for more information  |
| `ready-for-agent`            | `ready-for-agent`     | Fully specified, ready for an AFK agent   |
| `ready-for-human`            | `ready-for-human`     | Requires human implementation             |
| `wontfix`                    | `wontfix`              | Will not be actioned                     |
| —                            | `done`                 | Delivered and verified; no further action |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

`done` is the terminal state, and applies to a spec's `Status:` line as much as an issue's — a spec is `done` once every one of its issues is. The five roles above describe work that is still open, so none of them fits a finished ticket; a skill will never ask for `done` by role name, and it is set when the work lands rather than during triage.

Edit the right-hand column to match whatever vocabulary you actually use.
