---
description: A compensating event records, as a new fact, that an earlier event is being offset. Chronicle links the two by sequence number and never changes the original.
---
# Event compensation

Some domains cannot quietly correct a mistake. A ledger entry that was posted stays posted, and the correction is a second entry that offsets the first. A compensating event is that second entry: a new event that records which earlier event it compensates for.

Compensation is different from [revision](revision.md). A revision replaces how an event reads, in place, and keeps the original content beside it. A compensation adds a new event to the sequence and leaves the original event as it was.

## Declare a compensation event type

A compensation event type is an ordinary event type that says which event type it compensates for. Chronicle stores this on the event type's schema, so every client and the Workbench can discover which compensations exist for an event type. See [Compensating events in .NET](../clients/dotnet/compensation.md) for the attribute.

## Compensate an event

Compensating works like appending, with one difference: you name the event you are compensating instead of an event source. Chronicle then:

1. Looks up the event at that sequence number. If there is none, the compensation is rejected with `CompensatedEventNotFound`.
2. Checks that the compensation event type is declared for the type of that event. If it is not, the compensation is rejected with `EventTypeDoesNotCompensateEventType`.
3. Appends the new event to the same event source, event stream and subject as the original.
4. Stores the original's sequence number on the new event as `CompensationFor`.

Constraints, concurrency checks and compliance run for the compensating event like any other append. Observers receive it like any other event, and can read `CompensationFor` from the event context to relate the two.

Compensating never changes the original event. The same event can be compensated more than once if your domain allows it. Chronicle does not decide what a compensation means for your data; the compensating event's content and your observers do.

## Compensate from the Workbench

In the Workbench, select an event in the event sequence. When the event type has compensation event types registered, **Compensate** is available. Choose the compensation event type, fill in its content, and confirm. The new event appears in the sequence after the original.

## Wire values

On the wire, `CompensationFor` is the sequence number of the compensated event. Clients that cannot express an absent number read an event that is not a compensation as `0`. Use the .NET client, or check that the event's type is a compensation type, when that distinction matters.
