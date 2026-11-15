---
title: Cratis Chronicle event compensation for recorded mistakes
description: Cratis Chronicle appends a compensating event linked to an earlier event, leaving the original unchanged so your domain can record the correction.
---
Use event compensation in Cratis Chronicle when a correction belongs in the business history as a new fact. A posted ledger entry, for example, stays posted; a second entry offsets it. Chronicle links the compensating event to the original, while your event content and observers define what the correction does.

Compensation is different from [revision](revision.md). A revision replaces how an event reads, in place, and keeps the original content beside it. A compensation adds a new event to the sequence and leaves the original event as it was.

## Declare a compensation event type

A compensation event type is an ordinary event type declared to compensate for another type. Chronicle stores that relationship on the event type's schema. The Workbench uses it to offer the matching compensation types. To declare the relationship and append a compensation in C#, follow [Compensating events in .NET](../clients/dotnet/compensation.md).

## Compensate an event

Compensating works like appending, with one difference: you name the event you are compensating instead of an event source. Chronicle then:

1. Looks up the event at that sequence number. If there is none, the compensation is rejected with `CompensatedEventNotFound`.
2. Checks that the compensation event type is declared for the type of that event. If it is not, the compensation is rejected with `EventTypeDoesNotCompensateEventType`.
3. Appends the new event to the same event source, event stream and subject as the original.
4. Stores the original's sequence number on the new event as `CompensationFor`.

The compensating event goes through append validation, including constraints and compliance, but this operation does not request an optimistic concurrency check. Observers receive the new event and can read `CompensationFor` from its context to relate the two.

Compensation leaves the original event unchanged. Chronicle does not prevent multiple compensations for the same event, so enforce any domain rule that permits only one. Appending a compensation does not automatically reverse a payment, retract a message, or change a read model; your handlers must implement the intended effect.

## Compensate from the Workbench

In the Workbench, select an event in the event sequence. When the event type has compensation event types registered, **Compensate** is available. Choose the compensation event type, fill in its content, and confirm. The new event appears in the sequence after the original.

## Wire values

On the wire, `CompensationFor` is an optional sequence number identifying the compensated event. The .NET client preserves that absence as a nullable value. If a client represents absence as `0`, check that the event's type is a compensation type before interpreting the number as a link.
