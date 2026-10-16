<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/personas-and-causes.md -->
# Personas and causes

## Contents

- Role catalogue on `persona`
- What a Cannot line becomes
- Cannot resolved to an executable gate
- Checks while building
- Non-human causes (STATE.md, until a reaction or translation exists)

## Role catalogue on `persona`

Every actor the story mentions gets a catalogue entry. Human roles are `persona`
declarations; system actors (schedules, outside parties, our own follow-up work) are listed
as non-human causes below, by convention: the persona grammar does not forbid them (it checks
the name and declared policy references), but discovery keeps personas to people and defers the
reactions and translations that system actors imply to `cratis-screenplay-automations-and-translations`.

Each persona carries a `description` (documented in `personas.md` and `slices.md#descriptions`
of the Screenplay docs) with four parts:

- **purpose**: what the role is there to do or decide (first sentence);
- **Does**: decisions it takes in scope (each becomes a command later);
- **Reads**: what it needs to see to decide (each becomes a view later);
- **Cannot**: things it might plausibly try but must not do. Write "none known" rather than
  omitting it.

The role catalogue is mandatory: without Cannot lines there are no denial candidates, and
without Does and Reads lines later phases do not know which commands and views a role needs.

A multi-line description uses a fenced block (the fence is shown with four backticks so the
example can nest it; the complete documents below use the one-line form):

````screenplay excerpt
policy IsAuthenticated
  require authenticated
policy IsHarbourmaster
  require role "Harbourmaster"

persona Harbourmaster
  description
    ```text
    Decides who gets which berth and records arrivals and departures.
    Does: assigns or declines berth requests; withdraws an assignment before arrival.
    Reads: open requests by season; free berths by length.
    Cannot: set or waive season fees; request a berth on an owner's behalf.
    ```
  policy IsAuthenticated
  policy IsHarbourmaster
````

Parent document: the complete example under "Cannot resolved to an executable gate" below
declares the same policies and persona with the one-line form.

## What a Cannot line becomes

The persona block is report-only: Screenplay neither binds nor enforces it, and no rendered
code contains it (`description` text reaches generated code only as XML doc comments). Cannot text is intent, not
protection. Discovery lists the candidates; each stays pending until a later phase resolves it
into exactly one of these:

1. **Executable.** An `authorize` gate with a role, claim or authenticated policy the persona
   does not satisfy, or an ownership policy (`claim "x" matches subject|<command property>`),
   plus a `then denied` specification using `given caller as <Persona>` for a
   synthesized witness, or an explicit effective-gate caller (`cratis-screenplay-scenario-coverage`). Every command and query
   under an inherited module or feature `authorize` gets its own denial spec. Ownership
   against a text, Uuid, Date or DateTime subject renders; other subjects are STAGE-ESM-015: note it.
2. **Gap or target requirement.** The check depends on stored state ("only the requester may
   withdraw") or cannot be gated today. Record it as a gap with its target enforcement for
   `cratis-screenplay-streams-and-consistency`; it is not enforcement and not a passed denial.
3. **Scope disposition.** No command exists for it. Record that decision explicitly; it is a
   disposition, not proof of a denial.

Anything not yet resolved is an open question. Do not write Cannot lines for rules that bind
everyone (business rules, not authority).

A passing spec proves the gate refuses that caller, not every caller fitting a
persona. `given caller as <Persona>` chooses one authenticated policy witness;
inspect its `or` choice. Use explicit fixtures for anonymous callers, ownership
claims, missing gate conjuncts or refused synthesis. Name which witness stands
for which persona.

## Cannot resolved to an executable gate

Complete document: the persona's Cannot line "set or waive season fees" has no command yet
(a scope disposition), while "assign a berth" is limited to the Harbourmaster, so the Cannot
line for the BoatOwner on `AssignBerth` resolves to a gate and a denial spec.

```screenplay
domain Harbour.Moorings

concept BerthRequestId : Uuid
concept BerthNumber : String

policy IsAuthenticated
  require authenticated
policy IsHarbourmaster
  require role "Harbourmaster"

persona BoatOwner
  description "Asks for a berth for the season. Does: requests a berth. Reads: own requests. Cannot: assign a berth; see other owners' requests."
  policy IsAuthenticated
persona Harbourmaster
  description "Decides who gets which berth. Does: assigns berths. Reads: open requests. Cannot: set or waive season fees (no command yet: scope disposition)."
  policy IsAuthenticated
  policy IsHarbourmaster

module Moorings
  feature SeasonalBerths
    slice StateChange AssignBerth
      command AssignBerth
        berthRequestId BerthRequestId identifier
        berthNumber    BerthNumber
        authorize IsHarbourmaster
        produces event BerthAssigned
          berthNumber BerthNumber = berthNumber
      specification AssigningABerthAsHarbourmaster
        given caller
          authenticated
          role "Harbourmaster"
        when AssignBerth
          berthRequestId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          berthNumber    = "A-12"
        then BerthAssigned
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          berthNumber = "A-12"
      specification DenyingABoatOwnerAssigningABerth
        given caller
          authenticated
          role "BoatOwner"
        when AssignBerth
          berthRequestId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          berthNumber    = "A-12"
        then denied
```

Writing the denial spec belongs to scenario coverage; in discovery, record in STATE.md:

| Persona | Cannot line | Resolution (pending) |
|---|---|---|
| BoatOwner | assign a berth | gate `IsHarbourmaster` on `AssignBerth` + denial spec with role `BoatOwner` |
| BoatOwner | see other owners' requests | gap: depends on stored state (ownership of the request); target named in streams review |
| Harbourmaster | set or waive season fees | scope disposition: no command in this scope |

## Checks while building

- Two personas with the same Does lines: do they differ in authority? If not, merge.
- A Cannot line one of the persona's own policies would allow: open question.
- "Can someone act for another?" (office staff for an owner, a supervisor override): a Does
  line with its own policy, or a Cannot line.
- A role that only reads is fine; a role that neither does nor reads is a question.
- Every party named in a process is a persona or a non-human cause.

## Non-human causes (STATE.md, until a reaction or translation exists)

| Cause | Record |
|---|---|
| Schedule or deadline | what passes or recurs; whether the business must remember it passed |
| Outside party | who; what they tell us or we tell them; how it reaches us (they send it / we fetch it / a file / someone re-types it); how we know which of our records it concerns |
| Our own follow-up | which fact sets it off; what it decides |

Design mode: do not declare `system` blocks; outside parties stay in this table for
`cratis-screenplay-automations-and-translations`.
