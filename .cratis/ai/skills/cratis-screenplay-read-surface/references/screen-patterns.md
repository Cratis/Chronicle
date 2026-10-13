<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/screen-patterns.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Screen patterns

## Contents

- Input screen
- Status screen
- Error state
- A complete example
- What a screen pattern is not

Three screen shapes recur in almost every model. Decide which one a screen is before declaring
its `data` and `action` lines; the shape tells you which read model and which command it needs.
Adapted from Martin Dilger and Nebulit GmbH's storyboarding patterns (see `provenance.md`),
translated to `screen`, `data`, `action` and `specification`.

## Input screen

The person fills in a form, submits, and a fact is recorded.

- The form fields are the command's inputs; a field the person should not supply (their own
  identity, the tenant) comes from context, not the form (`cratis-screenplay-command-surface`
  `references/context.md`).
- The submit is `action <Command>` on the screen. The screen needs no read model when it is a
  blank creation form; when it shows current state before the person acts (the berth's
  availability, the amount still owed), it also needs `data <ReadModel> via query <Query>`.
- After success the person lands somewhere meaningful: `navigate to <Screen>` on the action, or the
  list that now contains the new item. Where the person goes next is part of the story, so name it.

## Status screen

The person looks at where something stands and decides what to do next.

- `data <ReadModel> via query <Query> [by <param>]` supplies the current state, built from the
  events so far. Every field shown has a source in that read model.
- The available actions depend on state. A static `action` line is always offered; an action that
  must differ by state is a guarded action (`references/guarded-actions.md`), never prose.
- A status screen with no incoming view is a gap; only a blank creation form is exempt.

## Error state

The person acts and the command is refused: no event is recorded, a message is shown, and the
person can retry or choose another action.

- The refusal is not drawn on the screen. It is behavior, pinned by a `specification` with
  `then error` (a rule) or `then denied` (a gate), and by the command's `validate` and `require`
  messages, which are the text the person sees.
- Name the way out. After a refusal the person can correct the input and submit again, or take a
  different action on the same screen. If neither is possible, the slice has a dead end; record
  it as an open question.
- Do not add a screen per error. One screen with its refusal specifications covers the states.

## A complete example

A berth booking form that shows availability (input screen over a view), a booking list that
shows state (status screen), and the refusal pinned by a specification (error state).

```screenplay
domain Harbourlight.Berths

concept BookingId : Uuid
concept BerthName : String
  validate
    not empty  message "A berth needs a name"

policy IsAuthenticated
  require authenticated

module Berths
  description "Berth bookings for one marina"
  authorize IsAuthenticated
  feature Bookings
    slice StateChange BookBerth
      description "A skipper books a berth"
      command BookBerth
        bookingId BookingId identifier
        berth BerthName
        produces event BerthBooked
          berth BerthName = berth
      screen BookBerthScreen
        title "Book a berth"
        action BookBerth
          navigate to OpenBookingsScreen
      specification BookingABerth
        given caller
          authenticated
        when BookBerth
          bookingId = "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a01"
          berth = "Pier 2, berth 14"
        then BerthBooked
          for "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a01"
          berth = "Pier 2, berth 14"
      specification BookingWithoutABerthNameIsRefused
        given caller
          authenticated
        when BookBerth
          bookingId = "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a02"
          berth = ""
        then error "A berth needs a name"

    slice StateView OpenBookings
      description "The harbour office lists the bookings that hold a berth"
      readmodel OpenBooking
        bookingId BookingId
        berth BerthName
      query ListOpenBookings => OpenBooking[]
      projection OpenBookings => OpenBooking
        from BerthBooked
          bookingId = $eventSourceId
          berth = berth
      screen OpenBookingsScreen
        title "Open bookings"
        data OpenBooking[] via query ListOpenBookings
        action BookBerth
```

## What a screen pattern is not

- Not a column or a lane: a screen is a declaration in a slice, not a position on a timeline.
  One screen per step is not a rule; a screen hosts every action the person can reasonably take
  there, and an action may be reachable from several screens.
- Not a mockup: a drawn page is never evidence for the model, and a Level 1 screen is a data and
  action contract that a renderer or a person fills in (`cratis-screenplay-ui-composition`).
- Not mandatory to render: a screen that stays design-only is still a contract for lineage and
  review.
