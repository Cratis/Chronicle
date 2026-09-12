---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/guards-and-fuses.md -->

# Guards, scans and fuses

A guard that cannot fail is worse than no guard: it converts "nobody looked" into a green
check. Every line is tagged **[contract]** (binding) or **[convention]** (the house
default) per the Three Levels of Authority in [`general.md`](./general.md).

## Non-vacuity

- **[contract] A scan over a possibly-empty population carries a non-vacuity check.**
  Assert the subject count is what you expect before believing the result, and fail when
  the population is unexpectedly empty.
- **[contract] Report the count on success.** "Checked 0 files, found 0 problems" and
  "checked 412 files, found 0 problems" are different verdicts and must read differently.
- **[contract] A pattern that matches nothing is a defect in the pattern** until proven
  otherwise. Prove a matcher still matches by planting a violation; see
  [`exit-codes-and-wrappers.md`](./exit-codes-and-wrappers.md).

## Allowlists

- **[contract] Every allowlist entry records its reason** — why this subject is exempt,
  and what would end the exemption.
- **[contract] Every allowlist entry has a sibling check** that fails when the entry
  becomes unnecessary, so the list shrinks instead of accumulating forever.
- **[convention] Prefer an expiry to a permanent exemption.** An entry nobody revisits is
  a rule quietly deleted.

## Destructive passes

- **[contract] Distinguish "subject set empty" from "qualifying set empty".** Finding no
  candidates at all is a different situation from finding candidates that none qualified;
  an unattended pass must refuse to proceed on the first.
- **[contract] Every unattended destructive pass carries a per-pass fuse** — a maximum
  number of subjects it may act on in one run, which stops the run rather than trimming
  the work silently.
- **[contract] Prepare the inverse before the forward action.** If an exact inverse or a
  safe compensation cannot be prepared, stop.
- **[contract] Re-read preconditions immediately before each mutation and stop when drift
  invalidates the authorized scope, safety assumptions, or recovery plan.** Benign drift
  within an already authorized bounded pass does not require another confirmation.
- **[convention] Dry-run output is the review artifact for a destructive pass whose exact
  targets were not already established in the conversation.** A user who has reviewed
  and authorized those targets is not asked to approve the same pass again.
