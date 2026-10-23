<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/berth-reservations.md -->
# Berth reservations: worked example

This is a legacy control without `command reads protected`; the unprotected-read
markers below describe that control, not all command reads. Eligible views can opt
into v25; the date-rule blocker still remains. Read
[protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md)
for admission and protected-read rendering (Stage 4.138.0; STAGE-ESM-044 otherwise).

Complete, compiled design-mode model (marina guest berths) showing a coverage matrix turned into specifications: happy path, one rejection per rule, authorization denial, duplicate and retry, competing claim, state violation, alternative branches, compensation and a view lifecycle family. The matrix for it is in `coverage-matrix.md`; the catalogue (`scenario-catalogue.md`) names its specifications.

```screenplay test=unbound
// Scenario coverage worked example: guest berths at a marina (complete document).
// Shows a coverage matrix turned into specifications: happy path, one rejection per rule,
// authorization denial, duplicate/retry, concurrent claim, state violation, alternative branches,
// compensation (cancellation frees the berth-night) and a view lifecycle family.
// Screens and personas are out of scope here (cratis-screenplay-slice-design). This is the canonical design-mode
// example: stored-state rules (the reservation must be live) are stated as `reads BerthBooking`
// + `require ... message` and marked NOT enforced in the slice descriptions, with their target
// named. Binding reports PLAY0271/PLAY0268 for them (expected; Screenplay#129): such a model
// is design-complete but not binding-ready, and that is reported, never "fixed" by deleting the
// rules. The past-date rule (`night >= today`) is declarative too: `today` has no runtime date
// value, so binding reports PLAY0268 on it (recorded as blocked, not moved into prose); the rule and its rejection
// specification stay so the intent executes once a date value exists.
// Verified: V1 (warnings as errors). V3 binding is blocked by the stored-state and date rules.

concept ReservationId : Uuid
concept BerthCode : String
  validate
    not empty  message "A berth code is required"

policy IsHarbourOffice
  require role "HarbourOffice"

module Berths
  feature GuestBerths
    authorize IsHarbourOffice

    slice StateChange ReserveBerth
      description "One guest boat per berth per night. OneBoatPerBerthNight declares that two event sources cannot hold the same berth-night; the competing-claim specification pins the loser's rejection after an earlier holder. It neither runs two commands at once nor proves the runtime's index-and-append behaviour under failure: that needs target-level verification. A retried ReserveBerth with the same reservation id is refused by ReserveOnce: the fact occurs once, but the retry is NOT answered with success, so a client that lost the first acknowledgement must treat this rejection as 'already reserved' (idempotent handling is a target decision, recorded as an open question). The night must not be in the past: declared in `validate` (`night >= today`) with its rejection specification. Binding reports PLAY0268 for the `today` operand, which has no runtime date value (comparisons between Date or DateTime command properties, or against literals, bind), so this rule is design-complete but blocks V3 until Screenplay supports `today`."
      command ReserveBerth
        reservationId ReservationId identifier
        berth         BerthCode
        night         Date
        boatName      String
        boatLength    Decimal
        validate
          boatLength > 0    message "Boat length must be positive"
          boatLength <= 24  message "Guest berths take boats up to 24 metres"
          night >= today    message "The night must not be in the past"
        produces event BerthReserved
          berth BerthCode = berth
          night Date = night
          boatName String = boatName
          boatLength Decimal = boatLength
      constraint OneBoatPerBerthNight
        unique berth, night on BerthReserved
        released by ReservationCancelled
      constraint ReserveOnce
        unique event BerthReserved

      specification ReservingAGuestBerth
        given caller
          authenticated
          role "HarbourOffice"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Silje"
          boatLength    = 11.5
        then BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5

      specification RejectingAZeroLength
        given caller
          authenticated
          role "HarbourOffice"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Silje"
          boatLength    = 0
        then error "Boat length must be positive"

      specification RejectingAnOversizedBoat
        given caller
          authenticated
          role "HarbourOffice"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Silje"
          boatLength    = 31
        then error "Guest berths take boats up to 24 metres"

      // Declarative past-date rule; binding is blocked for the `today` operand (see header).
      specification RejectingANightInThePast
        given caller
          authenticated
          role "HarbourOffice"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = "C7"
          night         = "2020-07-14"
          boatName      = "Silje"
          boatLength    = 11.5
        then error "The night must not be in the past"

      // Deliberate: concept rules get one rejection spec per concept, through one command.
      specification RejectingAnEmptyBerthCode
        given caller
          authenticated
          role "HarbourOffice"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = ""
          night         = "2027-07-14"
          boatName      = "Silje"
          boatLength    = 11.5
        then error "A berth code is required"

      specification ReservingABoatAtTheMaximumLength
        given caller
          authenticated
          role "HarbourOffice"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Silje"
          boatLength    = 24
        then BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 24

      specification RefusingASkipper
        given caller
          authenticated
          role "Skipper"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Silje"
          boatLength    = 11.5
        then denied

      specification RejectingASecondBoatForTheSameBerthNight
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c02"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Havbris"
          boatLength    = 9
        then error "Constraint 'OneBoatPerBerthNight' is violated: another event source already holds the constrained value."

      specification RejectingARetriedReservation
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Silje"
          boatLength    = 11.5
        then error "Constraint 'ReserveOnce' is violated: the event source already has the constrained event."

      specification ReservingABerthNightFreedByACancellation
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        when ReserveBerth
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c02"
          berth         = "C7"
          night         = "2027-07-14"
          boatName      = "Havbris"
          boatLength    = 9
        then BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c02"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Havbris"
          boatLength = 9

    slice StateChange CancelReservation
      description "Compensation for ReserveBerth: ReservationCancelled releases the berth-night claim. Cancelling and departing are mutually exclusive per reservation (CancelOrDepart). NOT enforced in the model today: a cancellation needs a live reservation. The `reads BerthBooking` + `require` below states the intent; it protects nothing (an unguarded materialized read). Target: Chronicle DCB on the reservation stream, or Arc `[ProtectedDecision]` + `DecisionRead<BerthBooking>`. The command specifications after a departure or cancellation pin that target validation message, which fires before the constraint; the constraint itself is tested separately with `when append`."
      command CancelReservation
        reservationId ReservationId identifier
        reason        String
        reads BerthBooking as booking by reservationId
        validate
          reason not empty  message "Say why the reservation is cancelled"
          require booking.status == "reserved"
            message "Only a live reservation can be cancelled"
        produces event ReservationCancelled
          reason String = reason
      constraint CancelOrDepart
        unique event ReservationCancelled
        unique event BoatDeparted

      specification CancellingAReservation
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when CancelReservation
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason        = "Weather"
        then ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"

      specification RejectingACancellationWithoutAReason
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when CancelReservation
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason        = ""
        then error "Say why the reservation is cancelled"

      specification RejectingASecondCancellation
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        when CancelReservation
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason        = "Weather"
        then error "Only a live reservation can be cancelled"

      specification RejectingACancellationAfterDeparture
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        when CancelReservation
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason        = "Changed plans"
        then error "Only a live reservation can be cancelled"

      // The CancelOrDepart constraint is only reachable at append time: a command first fails its own
      // live-reservation validation once the history has a departure or cancellation.
      specification AppendingACancellationAfterDeparture
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        when append ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Changed plans"
        then error "Constraint 'CancelOrDepart' is violated: the event source already has the constrained event."

      specification AppendingASecondCancellation
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        when append ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        then error "Constraint 'CancelOrDepart' is violated: the event source already has the constrained event."

      specification RefusingASkipperCancellation
        given caller
          authenticated
          role "Skipper"
        when CancelReservation
          reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason        = "Weather"
        then denied

    slice StateChange RecordDeparture
      description "Damage seen at departure is a separate fact the repair crew acts on. NOT enforced in the model today: a departure needs a live reservation, stated as `reads BerthBooking` + `require`. Target: Chronicle DCB or Arc `[ProtectedDecision]`."
      command RecordDeparture
        reservationId  ReservationId identifier
        damageObserved Bool
        reads BerthBooking as booking by reservationId
        validate
          require booking.status == "reserved"
            message "Only a live reservation can record a departure"
        produces event BoatDeparted
        produces when damageObserved == true
          BerthDamageReported
            for reservationId
      event BerthDamageReported

      specification RecordingACleanDeparture
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when RecordDeparture
          reservationId  = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          damageObserved = false
        then BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"

      specification RecordingADepartureWithDamage
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when RecordDeparture
          reservationId  = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          damageObserved = true
        then BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        then BerthDamageReported
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"

      specification RejectingARepeatedDeparture
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        when RecordDeparture
          reservationId  = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          damageObserved = false
        then error "Only a live reservation can record a departure"

      specification RejectingADepartureAfterCancellation
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        when RecordDeparture
          reservationId  = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          damageObserved = false
        then error "Only a live reservation can record a departure"

      specification AppendingARepeatedDeparture
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        when append BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        then error "Constraint 'CancelOrDepart' is violated: the event source already has the constrained event."

      specification AppendingADepartureAfterCancellation
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        when append BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        then error "Constraint 'CancelOrDepart' is violated: the event source already has the constrained event."

      specification RefusingASkipperDeparture
        given caller
          authenticated
          role "Skipper"
        when RecordDeparture
          reservationId  = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          damageObserved = false
        then denied

    slice StateView BerthBoard
      description "One row per live reservation; a cancelled reservation leaves the board"
      readmodel BerthBooking
        reservationId ReservationId
        berth         BerthCode
        night         Date
        boatName      String
        status        String
      query BerthBookingById => BerthBooking optional
        by reservationId ReservationId
      projection BerthBoard => BerthBooking
        from BerthReserved
          reservationId = $eventSourceId
          berth         = berth
          night         = night
          boatName      = boatName
          status        = "reserved"
        from BoatDeparted
          status = "departed"
        remove with ReservationCancelled

      specification BerthBoardLifecycle1Reserved
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        then query BerthBookingById
          arguments
            reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          result
            berth    = "C7"
            night    = "2027-07-14"
            boatName = "Silje"
            status   = "reserved"

      // Lifecycle2 and Lifecycle3 are sibling branches after Lifecycle1, not consecutive steps.
      specification BerthBoardLifecycle2Departed
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when append BoatDeparted
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        then query BerthBookingById
          arguments
            reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          result
            berth    = "C7"
            boatName = "Silje"
            status   = "departed"

      specification BerthBoardLifecycle3Cancelled
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        when append ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        then no readmodel BerthBooking for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"

      specification RemovingOnlyTheCancelledBooking
        given caller
          authenticated
          role "HarbourOffice"
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          berth      = "C7"
          night      = "2027-07-14"
          boatName   = "Silje"
          boatLength = 11.5
        given BerthReserved
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c02"
          berth      = "C8"
          night      = "2027-07-14"
          boatName   = "Havbris"
          boatLength = 9
        when append ReservationCancelled
          for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
          reason = "Weather"
        then no readmodel BerthBooking for "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        then query BerthBookingById
          arguments
            reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c02"
          result
            berth    = "C8"
            boatName = "Havbris"
            status   = "reserved"

      specification RefusingASkipperTheBoard
        given caller
          authenticated
          role "Skipper"
        then query BerthBookingById
          arguments
            reservationId = "6a1d2c3b-4e5f-4a6b-8c7d-9e0f1a2b3c01"
        then denied
```
