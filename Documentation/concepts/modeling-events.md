---
title: Modeling events well
description: The single most important skill in event sourcing — how to design events that are clear today and still make sense in five years.
---

Events are the foundation everything else is built on. Get them right and projections, reactors, and read models fall into place. Get them wrong and no amount of clever projection code will save you. This is the guide to getting them right.

The guiding principle behind all of it: **an event is a fact — an immutable record of something that happened.** Every rule below follows from taking that seriously.

## Name events as facts, in the past tense

An event states what *happened*, so its name is a past-tense verb phrase in the language of the domain: `OrderPlaced`, `AddressChanged`, `PaymentCaptured`, `BookReturned`. Not `CreateOrder` (that's a command — an intent), not `OrderState` (that's a model). If you can't name it in the past tense, it isn't an event yet.

```csharp
// ✅ A fact that happened
[EventType]
public record AddressChanged(Address Address);

// ❌ An intent (that's a command) or a state blob (that's a read model)
[EventType]
public record UpdateAddress(Address Address);
```

## One event, one purpose

Each event captures a single, meaningful change. Resist the "kitchen-sink" event that carries everything about an entity with most fields irrelevant on any given change. Multipurpose events force every consumer to figure out *which* change actually happened.

```csharp
// ❌ One event trying to be everything — consumers must guess what changed
[EventType]
public record CustomerUpdated(string? Name, Address? Address, Email? Email, bool? Deactivated);

// ✅ Distinct facts — each consumer subscribes to exactly what it cares about
[EventType] public record CustomerRenamed(CustomerName Name);
[EventType] public record AddressChanged(Address Address);
[EventType] public record CustomerDeactivated(DeactivationReason Reason);
```

## Never nullable — if it's optional, you need a second event

This is the rule that trips up newcomers most, and it's the most important. An event records what *was true* at the moment it happened. A nullable property means "this fact sometimes didn't happen" — which is a contradiction. If a value is sometimes present and sometimes not, that's **two different facts**, so model two events.

```csharp
// ❌ Nullable smell — "sometimes there's a discount, sometimes not"
[EventType]
public record OrderPlaced(OrderId Id, Money Total, Money? Discount);

// ✅ Two facts
[EventType] public record OrderPlaced(OrderId Id, Money Total);
[EventType] public record DiscountApplied(OrderId Id, Money Amount);
```

## Capture the decision, not the field write

The temptation coming from CRUD is to mirror table columns: a `Customer` changed, so emit `CustomerUpdated`. But the value of event sourcing is in the *meaning*. `AddressChanged` tells you a customer moved; a generic update tells you nothing. Model the business decision or transition, and the audit trail, analytics, and reactions have something precise to work from.

## Carry what was true then — and only that

An event holds the data that was true at the moment it occurred, captured by value. Don't reference mutable state that might change later, and don't enrich an event with data a consumer can derive itself. The event should be readable on its own, years from now, without joining against anything.

## The five-year test

Before you commit an event type, ask: **"Will this still make sense to someone reading it in five years, with no other context?"** Clear domain naming and self-contained data are what make that a *yes*. Events are forever — they're worth a minute of thought.

## Events change by migration, never by editing

When an event type needs to evolve, you don't rewrite history — you describe how to read old events as the new shape with an [event type migration](./event-type-migrations.md). Prefer *adding* a new event or field over overloading an existing event; it keeps each fact singular and clear. See the recipe: [Evolve an event's shape](../scenarios/evolve-an-event.md).

## Where this leads

- [Event](./event.md) and [Event Type](./event-type.md) — the mechanics.
- [Projections, reducers, and reactors](./observer-patterns.md) — what consumes your well-modeled events.
- [When to use event sourcing](./when-to-use-event-sourcing.md) — and when modeling events isn't worth it.
