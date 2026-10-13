<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/worked-example.md -->
# Worked example: reasoning documentation, constraint, decided rejection

A marina berth allocation feature. The feature `documentation` carries the reasoning (scope, an
assumption tied to Q1, a rule kept as a constraint). The refusal for a boat that already holds a
berth is a specification, not a note. The complete model validates and its three specifications pass on standalone
Screenplay 4.127.0; these are reference-runner checks, not rendered-code evidence.


````screenplay
domain Marina.Berths

concept BerthId : Uuid
concept BoatName : String

policy HarbourOffice
  require role "HarbourOffice"

persona HarbourMaster
  description "Allocates berths. Does: allocate a berth to a boat. Cannot: allocate without the HarbourOffice role."
  policy HarbourOffice

module Berths
  feature Allocation
    description "Allocating a berth to a boat"
    documentation
      ```markdown
      A boat holds one berth at a time; that rule is the OneBerthPerBoat
      constraint, not a second event type. Assumption Q1: allocation is
      always manual, so no automation is modeled.
      ```
    authorize HarbourOffice

    slice StateChange AllocateBerth
      command AllocateBerth
        berthId BerthId identifier
        boat    BoatName
        produces event BerthAllocated
          boat BoatName = boat

      constraint OneBerthPerBoat
        unique boat on BerthAllocated
        message "That boat already holds a berth"

      specification AllocatingABerth
        given caller
          authenticated
          role "HarbourOffice"
        when AllocateBerth
          berthId = "5d3c2a52-27c4-4d2b-a0ee-0a6a8a9f7b11"
          boat    = "Sea Wren"
        then BerthAllocated
          for "5d3c2a52-27c4-4d2b-a0ee-0a6a8a9f7b11"
          boat = "Sea Wren"

      specification RejectingASecondBerthForABoat
        given caller
          authenticated
          role "HarbourOffice"
        given BerthAllocated
          for "5d3c2a52-27c4-4d2b-a0ee-0a6a8a9f7b11"
          boat = "Sea Wren"
        when AllocateBerth
          berthId = "8e1f6c3a-6b7d-4c0e-9d5a-3f2b1a4c9e22"
          boat    = "Sea Wren"
        then error "That boat already holds a berth"

      specification RefusingACaller
        given caller
          authenticated
          role "Visitor"
        when AllocateBerth
          berthId = "5d3c2a52-27c4-4d2b-a0ee-0a6a8a9f7b11"
          boat    = "Sea Wren"
        then denied
````


## Why it is shaped this way
- The one-berth-per-boat rule (`unique boat` across berth event sources) is a constraint with a pinned message, so the competing-claim
  specification can state the exact rejection. No `BerthReassigned` event was added to carry it.
- The `Cannot` line in the persona resolves to the `HarbourOffice` policy and a `then denied`
  specification whose caller fixture carries a non-matching role.
- On Screenplay 4.144.0, record Q1 as a `question` note on `Berths.Allocation`:
  "Is allocation always manual? Assuming manual allocation; an automatic route
  would need an automation." The documentation carries the working assumption;
  STATE.md carries the returned note ID. Read `stop-or-assume.md` "Durable
  question notes" when recording or resolving it.
- The documentation is rationale only: the rules themselves are enforced by the constraint, the policy
  and the specifications, not by the prose.
- The constraint does not say a berth holds one boat at a time: a berth source may replace its
  claimed boat. If berth occupancy must be unique too, model and specify that as its own invariant.
