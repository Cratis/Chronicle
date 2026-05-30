---
title: "3. Reacting to events"
description: Run a side effect when something happens, using a reactor — and learn why reactors must be idempotent.
---

**What you'll do:** when a book is returned, notify the next member waiting for it. Projections build state; **reactors** *do things* — send mail, call an API, kick off another process.

## A reactor is just a class that observes events

`IReactor` is a marker interface. You don't implement a method from it — you write a method whose **first parameter is the event type** you care about, and Chronicle routes matching events to it:

```csharp
public class WaitlistNotifier(INotificationService notifications) : IReactor
{
    public async Task BookReturned(BookReturned @event, EventContext context)
    {
        // context.EventSourceId is the BookId the event happened to
        await notifications.NotifyNextInLine(context.EventSourceId);
    }
}
```

Chronicle discovers the reactor by convention — no registration ceremony.

## Why idempotency matters

A reactor may run **more than once** for the same event — during replay, recovery, or redeployment. So a reactor must be safe to repeat: notifying the same member twice is a bug. Design the side effect to be idempotent (for example, record that a notification was sent and skip if it already was).

:::caution
A reactor must never write to the event log directly. If reacting should produce new events, execute a command through the command pipeline instead. Reactors observe and cause side effects; they don't author history.
:::

## Use the event, don't query back

Everything the reactor needs is in the event and its `EventContext`. Resist the urge to query a read model from inside a reactor — the event already tells you what happened, and the read model may not have caught up yet (it's [eventually consistent](/chronicle/read-models/)).

## What you did

- Wrote a **reactor** that runs a side effect when `BookReturned` occurs.
- Learned the rules that keep reactors safe: **idempotent**, **no direct writes to the log**, **use the event data**.

## You've built a library

You now have the full loop: facts go in as **events**, a **projection** turns them into a queryable **read model**, and a **reactor** acts when something happens. That's event sourcing with Chronicle.

From here:

- Go deeper on each piece in [Concepts](/chronicle/concepts/) and the feature guides ([Projections](/chronicle/projections/), [Reactors](/chronicle/reactors/), [Reducers](/chronicle/reducers/)).
- Put a UI and commands on top with [Arc](/arc/) and [Components](/components/).
- Hit a wall? See [Troubleshooting](/chronicle/troubleshooting/).
