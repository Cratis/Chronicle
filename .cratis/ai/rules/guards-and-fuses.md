---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/guards-and-fuses.md -->

# Guards, scans and fuses

A guard that cannot fail is worse than no guard: it converts "nobody looked" into a green
check. Every line is tagged **[contract]** (binding) or **[convention]** (the house
default) per the Three Levels of Authority in [`general.md`](./general.md).

**Scope.** This rule governs two situations: *authoring* a scanner, guard, allowlist or
checker, and *running a pass* that mutates a computed set of subjects rather than the
targets the request named. An ordinary edit, review, fix, or a change to the one file the
user pointed at is outside it and carries none of the machinery below.

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

## Bulk and irreversible passes

These contracts are keyed to the *risk class* of the effect — bulk deletion, history
rewriting, cross-repository migration, anything irreversible or high-fanout — not to
whether a person is watching. An autonomous session runs them exactly as an interactive
one does.

- **[contract] Distinguish "subject set empty" from "qualifying set empty".** Finding no
  candidates at all is a different situation from finding candidates that none qualified;
  a pass must refuse to proceed on the first.
- **[contract] Every pass carries a per-pass fuse** — a maximum number of subjects it may
  act on in one run, which stops the run rather than trimming the work silently.
- **[contract] Know the recovery before the forward action.** For a reversible effect,
  know how it is undone. For an irreversible one, preserve what the recovery needs first —
  a backup ref, a copy under `.ai-work/keep/`, the list of targets — and stop if nothing
  can be preserved and the request did not name the targets. Do not demand an exact
  inverse where none can exist.
- **[contract] Re-read preconditions immediately before each mutation and stop when drift
  invalidates the authorized scope, safety assumptions, or recovery plan.** Benign drift
  within an already authorized bounded pass does not require another confirmation.
- **[convention] When the targets were not already established in the conversation, show
  the dry-run list and act on it once the user has answered.** A user who has reviewed
  and authorized those targets is not asked to approve the same pass again. The list is a
  message in the conversation, not a retained artifact: do not write receipts, ledgers,
  snapshots or escrow copies of it.
