---
title: React to an event
description: Run a side effect — send a notification, call an external system, trigger another command — when an event occurs.
---

**Goal:** when something happens — a book is returned, an order ships — you need to *do* something: notify a member, call an external API, or start another process. That's a [reactor](../reactors/).

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

## The three rules

1. **Be idempotent.** A reactor may run more than once for the same event (replay, recovery, redeploy). Make the side effect safe to repeat — record that it happened and skip if it already did.
2. **Don't write to the event log directly.** If reacting should produce *new* events, inject `ICommandPipeline` and execute a command. Reactors cause effects; they don't author history.
3. **Use the event, don't query back.** Everything you need is in the event and its `EventContext`; the read model may not have caught up yet ([eventual consistency](../read-models/)).

## Triggering another command

The common "translation" pattern — react to one slice's event by driving another slice's command:

```csharp
public class StockKeeping(ICommandPipeline commands) : IReactor
{
    public Task BookReserved(BookReserved @event, EventContext context) =>
        commands.Execute(new DecreaseStock(@event.Isbn));
}
```

## See also

- [Reactors](../reactors/) — the full reactor model, filtering, and once-only handling.
- [Projections, reducers, and reactors](../concepts/observer-patterns.md) — when a reactor is the right tool vs. building state.
