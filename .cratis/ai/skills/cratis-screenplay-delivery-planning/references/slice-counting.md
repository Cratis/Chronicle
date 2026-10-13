<!-- cratis-ai-managed: skills/cratis-screenplay-delivery-planning/references/slice-counting.md -->
# Slice counting

Read when counting, classifying or weighing the slices of an accepted model.

## What counts
One unit per slice of an accepted model. The type is the slice's declared type; the model decides it, not the planner.

| Type | What it delivers | Usually realized by |
|---|---|---|
| StateChange | a command, its rules and the events it produces | Stage renders it where admitted; else hand-written |
| StateView | a read model with its projection and query | Stage renders it where admitted; else hand-written |
| Automation | a reaction, todo list or trigger doing follow-up work | hand-written against the model (gap-fill) |
| Translate | a capture or translator turning outside data into events | hand-written against the model (gap-fill) |

Screens and UI composition belong to the slice whose data and actions they show. Count a screen separately only when the plan has a separate frontend owner, and then count it as its own line, not as a slice.

Count a rendered slice and a hand-written slice in different rows even when they share a type. The first row's effort is review, verification and customization; the second's is implementation.

## What not to count as slices
Plan these as separate lines, not slice weight:
- modeling, review and acceptance gates (lifecycle phases P0 to P6); P7 to P9 verification is part of each slice's done criteria,
- environment, hosting, deployment, identity and authorization provider setup,
- integration with an outside system beyond the slice's own adapter (credentials, sandboxes, contract tests with the other party),
- migration of existing data,
- documentation and training.

## Realization check
Before pricing a slice as rendered, run the admission probe in `cratis-screenplay-render-and-gap-fill` for the whole model. Do not restate version pins in a plan; cite `cratis-screenplay-toolchain` `references/versions.md` for the pair you used. A slice Stage refuses (an `STAGE-ESM-*` code) is hand-written work with the model as contract: price it as hand-written and record the refusal code.

## Weights
Weights are the team's, calibrated from completed slices, not constants of the method. Start with a simple scale and let history replace it:

| Class | Starting weight (first estimate without history) |
|---|---|
| rendered StateChange or StateView, no customization | 1 (the reference) |
| StateChange or StateView with customization or hand-written | 2 |
| Automation or Translate | 3 |
| any slice with an outside system on the critical path | add a separate integration line |

These starting weights are placeholders to be replaced. State them as assumptions in the plan. After the first completed groups, derive each class's weight as its median actual effort divided by the median effort of the reference class.

## Re-count triggers
Recount when: the model's slice set changes, a slice's type changes, a slice splits or merges, Stage admission changes after a tool upgrade, or a hand-written slice turns out renderable (or the reverse).
