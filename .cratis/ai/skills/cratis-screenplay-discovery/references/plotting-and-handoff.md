<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/plotting-and-handoff.md -->
# Plotting the story and handing it on

## Contents

- 1. Sequence
- 2. Record after / caused by / only if
- 3. Ask what can happen instead
- Durable discovery questions
- 4. Plot output format
- Whose turn is it in the story?
- Storyline: one view walked through its states
- 5. Plot checks
- Handoff packet to `cratis-screenplay-slice-design`

Plotting arranges the discovered events as a narrative: what happens first, what depends on
what, what can happen instead, where it ends. In Screenplay the plot is written into the
model (fenced `documentation` on slices and features) and into STATE.md. Board order is the
authored declaration and import order; it is presentation, not causation, so write causes and
dependencies down.

## 1. Sequence

Walk one workflow at a time, from its first trigger to every end. Ask, at each event:
what came before it, what made it happen, what is it only allowed under?

## 2. Record after / caused by / only if

Write these three into the `documentation` of the `StateChange` slice that produces the event:

```text
After:     BerthRequested
Caused by: Harbourmaster (decides)
Only if:   a free berth is at least as long as the boat (intent, not enforced in the model today)
```

- **After** is the story predecessor. A predecessor is not the same as a cause; record both.
- **Caused by** is a persona's action, a schedule, an outside party or another fact.
- **Only if** is intent. Later phases turn it into a rule layer, `reads` + `require`, a
  constraint, or a recorded target requirement (`cratis-screenplay-slice-design`,
  `cratis-screenplay-streams-and-consistency`), and into state-violation specs
  (`cratis-screenplay-scenario-coverage`). A rule that depends on stored state is stated
  intent and is marked not enforced in the model today; never copy state into a command input
  to make it look enforced.

A starting event has no *after*; it still has a cause.

## 3. Ask what can happen instead

At every event, not only at forks someone mentions:

- What can happen next *instead*, including nothing? Does "nothing" expire, or wait forever?
- Which next steps may happen in any order, and which must wait for each other? Unordered
  successors become automations or ordering specs later; say so in the feature `documentation`.
- On failure or refusal, what compensates, and is the refusal itself a fact the business must
  remember (a decision) or just a rejection (a scenario later)?

Classify each fork: *outcome branch* (one decision, mutually exclusive results: same feature,
sibling events) or *separate story* (the choice is made before the process starts and
everything after differs: its own feature).

Name every terminal state (completed, cancelled, expired, withdrawn) and every undo or
correction path. Each path with a way out feeds removal specs later (an ending removes the
thing from the views that listed it).

## Durable discovery questions

Every unsettled branch, dead end or assumption becomes a `question` note on the
affected element, not chat or `description` text. Commit `.screenplay/notes.json`
with the model. STATE.md and the plot report carry the returned note ID and
assumption, not the only copy of the question. Before a specific element exists,
anchor to the application or nearest identified container and name it in text.
Never put secrets or personal data in note text, author, refs or resolutions.

Read model-authoring [model notes](../../cratis-screenplay-model-authoring/references/model-notes.md)
when recording or resolving discovery questions: `list-model-notes`,
`propose-model-notes`, `read-proposal view=notes`, then authorized `apply`.
A missing notes capability is a reported gap plus a note request, not a successful
recording. For project decisions, cite `refs: [{ "id": "D123" }]`; resolve with
`by: "external-decision"` and `ref: { "id": "D123" }` when that record answers it.
Turn the answer into a specification, a description or a project-owned decision
record. A resolution alone implements nothing; carry behavioral answers to slice
design/scenario coverage. Reopen if the decision stops answering the question.
Evidence: Screenplay `v4.144.0:Documentation/screenplay/model-notes.md:12-77`,
`decisions/0045-model-notes-sidecar.md:40-79`; tool-source lines in the linked guide.

## 4. Plot output format

Put a compact story line in the feature `documentation` and report the plot per workflow:

```text
The plot: SeasonalBerths

1. BerthRequested         start; caused by BoatOwner
2. BerthAssigned          after 1; caused by Harbourmaster; only if a free berth fits
   BerthRequestDeclined   after 1; caused by Harbourmaster (instead of 2)
3. BoatArrived            after 2; caused by Harbourmaster
4. BoatDeparted           after 3; caused by schedule (season end)   terminal
   BerthAssignmentWithdrawn after 2, before 3; caused by Harbourmaster   terminal

Critical path:  1 -> 2 -> 3 -> 4
Decision points: after 1 (assign or decline); after 2 (arrive or withdraw)
Terminal facts: BerthRequestDeclined, BoatDeparted, BerthAssignmentWithdrawn
Wait states:    after 2, until the boat arrives (report Q1: what if it never does?)
Compensation:   report Q2: does withdrawal free the berth for the waiting list?
```

Q1 and Q2 are report shorthand for anchored `question` notes; do not copy
unanswered questions into the feature's `documentation`. The settled plot goes
there. For a diagram use `cratis-event-model-diagram`.

## Whose turn is it in the story?

Tell the story from one persona's perspective as far as the process allows. At each event ask
whose turn it is. While it is still the same persona, stay there: a long stretch of events
belonging to one persona, with another persona's views changing in the background, is normal.
Switch only at a genuine handoff (a boat owner requests a berth, then the Harbourmaster must
act on it) or as a deliberate cutaway to show what another persona sees. A change to another
persona's read model is not automatically a new beat; it earns one only when that persona
would look at it as the next step. Do not alternate personas for symmetry. Carry each real
handoff into the feature `documentation` and the slice-design handoff packet; a cutaway is
marked as such there.

```text
Consistent:  BoatOwner: BerthRequested, RequestWithdrawn... then Harbourmaster: BerthAssigned, BoatArrived
Alternating: Harbourmaster, BoatOwner, Harbourmaster, BoatOwner with no handoff behind each switch
```

## Storyline: one view walked through its states

When the story is about a *view* rather than a stream (a waiting list, a work queue, a berth
board), a set of isolated before/after pairs hides what the user cares about: the same read
model changing over time. Note it as one narrated walkthrough in STATE.md and the feature
`documentation`: empty, then an item added, then the item done or failed, then removed, naming
the event that causes each step. It is a seed only; discovery writes no specification.
`cratis-screenplay-scenario-coverage` turns it into cumulative lifecycle specifications on
the read model, and `cratis-screenplay-automations-and-translations` owns the work-queue
(todo-list) shape. Each step must be caused by a modeled event; a step with no cause is an
open question.

```text
Waiting list (view): empty
  BerthRequested (Dana, 9 m)        -> one row, waiting
  BerthRequested (Ola, 12 m)        -> two rows
  BerthAssigned (Dana)              -> Dana leaves the list
  BerthAssignmentWithdrawn (Dana)   -> open question: does Dana return to the list?
```

## 5. Plot checks

- Every event except a starting one has a predecessor *and* a cause.
- Dependencies and alternative paths are written down, not implied.
- Terminal states are clear; compensation and cancellation flows are complete.
- Every brainstormed event appears in the plot or in the set-aside list with the reason.
- The plot makes business sense when read aloud in order.

## Handoff packet to `cratis-screenplay-slice-design`

The structure and section order of the packet are in `cratis-screenplay-modeling-lifecycle`.
What discovery contributes:

| Section | Content |
|---|---|
| Workflows covered | per feature: goal, happy path, decision points, terminal facts |
| Subjects | what each event story is about and what tells two apart (for later `for`/streams) |
| Rules register | each rule, the slice it constrains, intent text, target enforcement if state-dependent |
| Denial candidates | every Cannot line with its pending resolution |
| Competition candidates | scarce things two parties may want at once; who wins |
| Duplicate candidates | requests that may arrive twice |
| Non-human causes | schedules, outside parties, own follow-up |
| Hotspots | where it goes wrong today |
| Set aside | non-events kept as view needs |
| Open questions | anchored note IDs and assumptions; the blocking one first |
| Decisions and assumptions | kept apart; each assumption with its reason |
| Verdicts | V1 result with tool and version; V2 to V5 "not run: discovery phase" |

Seeds for later skills:

| Discovery item | Becomes (not decided here) |
|---|---|
| Cannot line | a gate and `then denied` spec, or a recorded gap |
| "Only if" | a rule layer / `require` / constraint, or a recorded target requirement |
| Competition candidate | a competing-claim spec; consistency design |
| Duplicate candidate | a duplicate or retry spec |
| Ending | removal from affected views |
