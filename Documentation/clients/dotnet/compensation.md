---
description: Declare compensating event types with CompensationFor and append them with IEventSequence.Compensate in the Chronicle .NET client.
---
# Compensating events in .NET

A compensating event records that an earlier event is being offset. For the concept and the rules Chronicle applies, see [Event compensation](../../events/compensation.md).

## Declare a compensation event type

Mark the event type with `CompensationFor<TEventType>`, naming the event type it compensates for:

```csharp
[EventType]
public record EntryPosted(decimal Amount);

[EventType, CompensationFor<EntryPosted>]
public record EntryReversed(string Reason);
```

Chronicle registers the link with the event type's schema. A compensation event type can compensate for one event type.

## Append a compensation

Call `Compensate` on the event sequence with the sequence number of the event to compensate for:

```csharp
var result = await eventLog.Compensate(sequenceNumber, new EntryReversed("Posted to the wrong account"));
```

The event goes to the event source, event stream and subject of the event being compensated, so you do not pass them. The result is an `AppendResult`. A rejected compensation reports `AppendError.CompensatedEventNotFound` or `AppendError.EventTypeDoesNotCompensateEventType` in `result.Errors`.

## Read the link

Observers and readers get the compensated sequence number from the event context:

```csharp
if (context.CompensationFor is { } compensated)
{
    // This event compensates for the event at `compensated`.
}
```

`CompensationFor` is `null` for events that are not compensations. Event sequence implementations that do not support compensations, such as the no-op event log used in tests, throw `CompensationsNotSupported` from `Compensate`.
