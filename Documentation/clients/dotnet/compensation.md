---
title: Cratis Chronicle event compensation in .NET
description: Declare a Cratis Chronicle compensation event type and append a correction linked to the original event's sequence number, without changing the original.
---
Cratis Chronicle event compensation appends a new event that offsets an earlier event without changing it. Use compensation when the correction must remain part of the record, such as a reversal of a posted ledger entry. Your observers decide how that correction affects application state.

## Declare a compensation event type

Mark the correcting event type with `CompensationFor<TEventType>` alongside `EventType`. In this example, `EntryReversed` can compensate an `EntryPosted` event:

```csharp
[EventType]
public record EntryPosted(decimal Amount);

[EventType, CompensationFor<EntryPosted>]
public record EntryReversed(string Reason);
```

Chronicle registers the link with the event type's schema, not as a content property on each event. A compensation event type can compensate for one event type. `Reason` describes this reversal; you do not add the original event's sequence number to the record type.

## Append a compensation

Call `Compensate` on the sequence that holds the original event. Pass that event's sequence number and the new correction:

```csharp
var result = await eventLog.Compensate(sequenceNumber, new EntryReversed("Posted to the wrong account"));
```

Check `result.IsSuccess` before treating the correction as recorded. `Compensate` returns an `AppendResult`, like `Append`; the attribute alone does not make the append succeed.

You do not pass an event source, event stream, or subject because the new event inherits those from the original. A rejected compensation reports `AppendError.CompensatedEventNotFound` if the original does not exist, or `AppendError.EventTypeDoesNotCompensateEventType` if the correcting type does not compensate the original's type. Inspect `result.Errors` for these errors. Normal append constraints and compliance still apply.

## Read the link

Observers receive the correcting event like any other append. Read `EventContext.CompensationFor` to find the original event's sequence number:

```csharp
if (context.CompensationFor is { } compensated)
{
    // This event compensates for the event at `compensated`.
}
```

`CompensationFor` is `null` for events that are not compensations. Recording a correction does not itself reverse an external action. For how observers interpret compensations and how to create one in the Workbench, see [Event compensation](../../events/compensation.md).

Event sequence implementations that do not support compensations, such as the no-op event log used in tests, throw `CompensationsNotSupported` from `Compensate`.
