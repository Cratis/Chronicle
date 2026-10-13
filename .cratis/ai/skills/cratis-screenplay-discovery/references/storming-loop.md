<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/storming-loop.md -->
# The storming loop on `.play`

## Contents

- One turn
- Choosing the next question
- When to stop
- A discovery skeleton

The model is the whiteboard. Every piece of input is turned into model edits first; the chat
then reports what changed and asks one question. The user should always be able to look at the
`.play` source and see the conversation so far.

## One turn

1. **Read the input.** Any form works: a sentence, a pasted ticket, meeting notes, a process
   description, a spreadsheet header row.
2. **Pull out candidates.** Look for things that became true and stay true: decisions with an
   outcome (approved, declined, waived), hand-overs between parties, milestones, deadlines that
   passed, corrections. Ignore how screens or systems work.
3. **Filter and name.** Run each candidate through `event-naming.md`. Rename quietly to a good
   business name; do not lecture. Put non-events aside (they often become views later) and,
   if they look important, ask what changed when that happened.
4. **Classify each edit against the current model:**

   | Edit | Meaning | Note |
   |---|---|---|
   | add | a new fact | new `StateChange` slice named after the probable command, event only |
   | rename | better business name for an existing fact | identity-affecting once persisted: see SKILL.md |
   | split | one name was covering two facts | keep the original if it is persisted; add the second |
   | merge | two names for one fact | only when they truly mean the same thing |
   | reorder | the story order changed | change declaration/import order for the board; update the feature `documentation`/STATE.md for causes and branches |
   | drop | not a fact after all | record why in STATE.md |
   | none | already modeled | say so briefly |

5. **Write the edits** with essential fields (identity is implicit; the one or two facts that make the event meaningful, none for a plain transition; never padding).
6. **Validate after a coherent batch** (`--warnings-as-errors`), not after every sentence.
7. **Report the change** in a few lines, then ask **one** question.

Report shape:

```text
Added:    BerthRequested, BerthAssigned (SeasonalBerths)
Renamed:  BoatArrivedInHarbour -> BoatArrived
Set aside: "harbourmaster checks the waiting list" (a view, not a fact)
Question: When the season ends, does the boat have to leave, or can the berth roll over?
```

## Choosing the next question

Prefer, in this order: the missing start trigger; a missing ending; a failure that has no
fact; who causes an event nobody has claimed; a rule that would change the shape of an event.
Do not ask for detail (field formats, screen layouts) during storming. When none of these is
open, start the sweep (`divergent-sweep.md`).

## When to stop

- Attended: the user says the workflow is complete, or the two closing questions ("Is there
  anything that happens here that we have not written down?" and "Which open question blocks
  the next step?") return nothing new.
- Unattended: every sweep lens has an outcome and the Gate in SKILL.md holds. Do not keep
  inventing facts to fill a lens.
- Never stop on a failing compile; fix or report it first.

## A discovery skeleton

What a model looks like after the first workflow has been stormed: no commands, read models
or screens yet. Slices are named after the command expected to produce each event. At
Screenplay 4.125.0, boards use authored declaration/import order, and MCP `expand-layout`
preserves it with imports. An import-less `PlayFileWriter.Expand` layout still falls back to
path order. Order is presentation only; the feature `documentation` records causes and branches.
Persona `description` text holds Does / Reads / Cannot on one quoted line here; the multi-line fenced
form is in `personas-and-causes.md`.

```screenplay
domain Harbour.Moorings

concept BoatName : String
concept LengthInMetres : Decimal
  validate
    min 1  message "A boat is at least one metre long"
concept SeasonYear : Int

policy IsAuthenticated
  require authenticated
policy IsHarbourmaster
  require role "Harbourmaster"

persona BoatOwner
  description "Asks for a berth for the season and keeps track of the answer. Does: requests a berth; withdraws a request before it is answered. Reads: the state of own requests. Cannot: choose a berth; see other owners' requests."
  policy IsAuthenticated
persona Harbourmaster
  description "Decides who gets which berth and records arrivals and departures. Does: assigns or declines berth requests; withdraws an assignment before arrival. Reads: open requests by season; free berths by length. Cannot: set or waive season fees."
  policy IsAuthenticated
  policy IsHarbourmaster
persona FeeAuditor
  description "Reads berth occupancy each quarter to check fees; never changes anything. Does: nothing. Reads: occupancy by season. Cannot: change any berth or fee."
  policy IsAuthenticated

module Moorings
  description "Seasonal berths in one marina"
  feature SeasonalBerths
    description "Story: BerthRequested, then BerthAssigned or BerthRequestDeclined; BoatArrived; BoatDeparted at season end, or BerthAssignmentWithdrawn before arrival"
    slice StateChange RequestBerth
      event BerthRequested
        boatName BoatName
        length   LengthInMetres
        season   SeasonYear
    slice StateChange AssignBerth
      description "After BerthRequested; the Harbourmaster decides. Only if a free berth is at least as long as the boat (intent, not enforced in the model today)"
      event BerthAssigned
        berthNumber String
    slice StateChange DeclineBerthRequest
      event BerthRequestDeclined
        reason String
    slice StateChange RecordArrival
      event BoatArrived
    slice StateChange RecordDeparture
      event BoatDeparted
    slice StateChange WithdrawBerthAssignment
      event BerthAssignmentWithdrawn
        reason String
```

Record anchored `question` notes for these uncertainties, carrying their note IDs
in STATE.md: is a declined request final or can it be
put on a waiting list; who may withdraw an assignment; must the season fee be remembered as
set at assignment time (a decision, so a fact) or recalculated (a view)?
