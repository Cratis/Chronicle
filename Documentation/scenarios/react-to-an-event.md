---
title: React to an event
description: Run a side effect — send a notification, call an external system, append a follow-up event — when an event occurs.
---

**Goal:** when something happens — a book is returned, an order ships — you need to *do* something: notify a member, call an external API, or record a follow-up fact. That's a [reactor](../reactors/).

## Write the reactor

A reactor is a class implementing the marker interface `IReactor`. You don't implement a method from it — you write a method whose **first parameter is the event type** you want to handle, and Chronicle routes matching events to it:

```csharp
public class WaitlistNotifier(INotificationService notifications) : IReactor
{
    public async Task BookReturned(BookReturned @event, EventContext context)
    {
        // context.EventSourceId is the source the event happened to (the book)
        await notifications.NotifyNextInLine(context.EventSourceId);
    }
}
```

Chronicle discovers it by convention — no registration.

## Be safe to repeat

A reactor may run more than once for the same event (replay, recovery, redeploy). Make the side effect idempotent — record that it happened and skip if it already did. For a side effect that must never run again during a replay — a welcome email, a payment call — mark the handler (or the whole class) with `[OnceOnly]` and Chronicle excludes it from replays. See [OnceOnly](../reactors/once-only.md).

## Need state? Pick the right consistency

Reach for the event first — it carries the truth of what happened, and `context.EventSourceId` tells you what it happened *to*. But some reactions genuinely need more state. Say the notification should include the book's title — that lives in the read model, not the event.

:::tip
You *can* read state from a reactor — just mind the consistency level. The **materialized** read model (what the projection writes to the sink) is [eventually consistent](../read-models/consistency.md) and may not have caught up with the very event you're reacting to. `IReadModels.GetInstanceById` is **strongly consistent**: it rebuilds the instance from the event log on demand, so it includes the event in your hand.
:::

```csharp
public class WaitlistNotifier(IEventStore eventStore, INotificationService notifications) : IReactor
{
    public async Task BookReturned(BookReturned @event, EventContext context)
    {
        // Strongly consistent — rebuilt from the event log, includes this event
        var book = await eventStore.ReadModels.GetInstanceById<Book>(context.EventSourceId);
        await notifications.NotifyNextInLine(context.EventSourceId, book.Title);
    }
}
```

The full menu of read APIs — single, all, paged, observed — is in [Get read models](./real-time-query.md).

## Appending an event

The common "translation" pattern — react to one slice's event by recording a new fact. The simplest way is to **return the event**: Chronicle appends it to the event log for you, against the triggering event's `EventSourceId`:

```csharp
public class StockKeeping : IReactor
{
    public StockDecreased BookReserved(BookReserved @event, EventContext context) =>
        new(@event.Isbn, 1);
}
```

`Task<StockDecreased>` and collections of events work too, and you can control the target event source — see [Returning side effects](../reactors/side-effects.md).

When you want to inspect the outcome yourself, append explicitly and handle the `AppendResult` — and **throw if it failed**, so the partition pauses instead of the fact being silently lost:

```csharp
public class StockKeeping(IEventStore eventStore) : IReactor
{
    public async Task BookReserved(BookReserved @event, EventContext context)
    {
        var result = await eventStore.EventLog.Append(
            context.EventSourceId, new StockDecreased(@event.Isbn, 1));
        if (!result.IsSuccess)
        {
            throw new StockCouldNotBeDecreased(@event.Isbn);
        }
    }
}
```

## See also

- [Reactors](../reactors/) — the full reactor model, filtering, and once-only handling.
- [Returning side effects](../reactors/side-effects.md) — every supported return shape and metadata control.
- [Projections, reducers, and reactors](../concepts/observer-patterns.md) — when a reactor is the right tool vs. building state.
