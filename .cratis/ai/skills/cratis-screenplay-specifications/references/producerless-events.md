<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/producerless-events.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Events without a producing command

Requires Screenplay 6.15.0 or later (`screenplay --version`). Source: Screenplay
`Documentation/screenplay/specifications.md`, "Event sources without one producing command".

A `given <Event> for <literal>`, a `when append <Event> for <literal>`, a named event example and
`for case.<parameter>` need an event-source identity type. A single producing command, or an
explicit route, supplies it and wins first. When no single producer supplies one distinct
destination type, a root read-model projection that consumes the event can supply it instead. Never
add a producer command only to make a specification compile: the model would then claim a
behavior nobody asked for.

The projection supplies the identity type when:

- it consumes the event by the event's own event source (the default key or `$eventSourceId`),
  never by an event payload property;
- every matching root projection has exactly one required scalar read-model key, and all those keys
  have the same type;
- the `for` value is a literal that fits that type.

Protected (`pii` or `secret`), optional, collection, structured, missing or conflicting identity
types stay refused, and an optional `for case.<parameter>` cannot supply a required identity even
when one row holds non-null text. When no identity type can be determined, `PLAY0273` names the
event and asks for a projection keyed on the event source or a producing command with a required
scalar destination. The `for` syntax and the selected ESM version do not change.

A view that counts slots per day, specified with `then readmodel`; a `then query` over `DayById`
asserts the same row. Both facts fold into one instance, so initialize required counters in the
projection that establishes the row:

```screenplay
module Demo
  feature Days
    slice StateView Calendar
      event DayOpened
        dayId String
      event SlotTaken
        dayId String
      readmodel DayView
        dayId String
        slots Int
      query DayById => DayView optional
        by dayId String
      projection DayProjection => DayView
        from DayOpened
          dayId = dayId
          slots = 0
        from SlotTaken
          increment slots
      specification CountingSlots
        given DayOpened
          for "2026-11-10"
          dayId = "2026-11-10"
        given SlotTaken
          for "2026-11-10"
          dayId = "2026-11-10"
        then readmodel DayView
          dayId = "2026-11-10"
          slots = 1
```
