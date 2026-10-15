<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/intent-example.md -->
# Intentional candidate example

A complete, compiling example of what extraction produces: one legacy write that touched two
tables (`Checkins`, `Hooks`) is re-sliced into a single decision event plus a projection. The
numbers in the slice descriptions (`E1`, `E6`, `Q3`) are evidence ids and open questions from
the evidence table (`evidence-table-template.md`) and `LOSS.md`.

What to notice:

- One StateChange per decision; the `Hooks` update is a consequence, so it is a projection
  (`CoatStatus`), not a second event.
- The unique index on the ticket number became a `unique ... on CoatTakenIn` constraint with
  `released by CoatHandedBack`. Releasing it on hand-back is an assumption pending expert
  approval (Q3), and the description says so.
- The state-dependent rule (a coat can only be handed back while on a hook) is written as
  `reads CoatStatus` + `require`, marked **NOT enforced in the model today** in the slice
  description, with its target named. No error specification is written for it.
- Every role gate found in code has `authorize` through the module policy and a `then denied`
  specification.
- The nightly hook reset (E4) is unexplained, so it is not modeled; it stays an open question
  (Q5) in the loss report.

```screenplay test=unbound
// An intentional candidate re-sliced from as-is evidence. DESIGN MODE.
// Evidence ids refer to rows like those in evidence-table-template.md: the legacy
// POST /api/checkins inserted Checkins and updated Hooks (E1); here that becomes one
// decision event, and hook occupancy becomes a projection.
domain Harbor.Cloakroom

concept CoatId : Uuid
concept TicketNumber : String
  validate
    not empty  message "A coat needs a ticket number"

policy IsAttendant
  require role "Attendant"

module Cloakroom
  description "Coats held for guests. Candidate from legacy extraction; see LOSS.md for what is not known"
  authorize IsAttendant
  feature Intake
    slice StateChange TakeInCoat
      description "An attendant hangs a guest's coat and issues a ticket. Evidence: E1 (POST /api/checkins; the Hooks update is a consequence, see CoatStatus), E6 (unique index on Checkins.TicketNumber, message read in CheckinService.cs:52-60). Legacy answered duplicates with HTTP 409 before the 403 role check; Q3 (open): may ticket numbers be reused after a hand-back? Releasing the constraint on CoatHandedBack, i.e. allowing reuse, is a proposed candidate assumption pending expert approval, not established legacy behaviour"
      command TakeInCoat
        coatId       CoatId identifier
        ticketNumber TicketNumber
        produces event CoatTakenIn
          ticketNumber TicketNumber = ticketNumber
      constraint UniqueTicketNumber
        unique ticketNumber on CoatTakenIn
        released by CoatHandedBack
        message "That ticket number is already in use"
      specification TakingInACoat
        given caller
          authenticated
          role "Attendant"
        when TakeInCoat
          coatId       = "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
          ticketNumber = "T-551"
        then CoatTakenIn
          for "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
          ticketNumber = "T-551"
      specification RejectingADuplicateTicketNumber
        given caller
          authenticated
          role "Attendant"
        given CoatTakenIn
          for "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
          ticketNumber = "T-551"
        when TakeInCoat
          coatId       = "7c6b5a49-3827-4615-a4b3-c2d1e0f9a002"
          ticketNumber = "T-551"
        then error "That ticket number is already in use"
      specification RejectingAnEmptyTicketNumber
        given caller
          authenticated
          role "Attendant"
        when TakeInCoat
          coatId       = "7c6b5a49-3827-4615-a4b3-c2d1e0f9a002"
          ticketNumber = ""
        then error "A coat needs a ticket number"
      specification RefusingAGuestTakingInACoat
        given caller
          authenticated
          role "Guest"
        when TakeInCoat
          coatId       = "7c6b5a49-3827-4615-a4b3-c2d1e0f9a002"
          ticketNumber = "T-552"
        then denied

  feature Collection
    slice StateChange HandBackCoat
      description "The guest returns the ticket and gets the coat. Evidence: E7 (POST /api/checkins/{id}/handback updated Checkins and Hooks), E2 (CheckinService.cs:88, 409 'That coat is not on a hook'). The rule E2 depends on stored state: it is stated as reads + require and is NOT enforced in the model today (PLAY0271 at binding in design mode); target enforcement is a protected decision or a Chronicle concurrency scope on the coat's stream. No error specification is written for it because the model cannot execute it. Q2: are damaged or unclaimed coats a separate fact?"
      command HandBackCoat
        coatId CoatId identifier
        reads CoatStatus by coatId
        validate
          require CoatStatus.onHook == true
            message "That coat is not on a hook"
        produces event CoatHandedBack
      specification HandingBackACoat
        given caller
          authenticated
          role "Attendant"
        given CoatTakenIn
          for "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
          ticketNumber = "T-551"
        when HandBackCoat
          coatId = "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
        then CoatHandedBack
          for "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
      specification RefusingAGuestHandingBackACoat
        given caller
          authenticated
          role "Guest"
        when HandBackCoat
          coatId = "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
        then denied

    slice StateView CoatStatus
      description "Replaces the legacy Hooks.occupied column (E1, E7), which only ever followed intake and hand-back. The nightly hook reset (E4) is unexplained and not modeled; see question Q5"
      readmodel CoatStatus
        coatId       CoatId
        ticketNumber TicketNumber
        onHook       Bool
      query CoatById => CoatStatus optional
        by coatId CoatId
      projection CoatStatus => CoatStatus
        from CoatTakenIn
          coatId       = $eventSourceId
          ticketNumber = ticketNumber
          onHook       = true
        from CoatHandedBack
          onHook = false
      specification SeeingAHandedBackCoatAsOffTheHook
        given caller
          authenticated
          role "Attendant"
        given CoatTakenIn
          for "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
          ticketNumber = "T-551"
        given CoatHandedBack
          for "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
        then query CoatById
          arguments
            coatId = "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
          result
            coatId       = "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
            ticketNumber = "T-551"
            onHook       = false
      specification RefusingAGuestTheCoatStatus
        given caller
          authenticated
          role "Guest"
        then query CoatById
          arguments
            coatId = "3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b001"
        then denied
```
