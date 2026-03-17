---
name: add-reactor
description: Use this skill when asked to add a Chronicle reactor (automation or translation) to a Cratis-based project. Reactors observe events and produce side effects.
---

Add a Chronicle **reactor** that triggers automation or translation logic in response to events.

> **Always read `.github/instructions/reactors.instructions.md` first.** It is the source of truth for reactor conventions, rules, and patterns.

## Step 1 — Identify the event(s)

Determine which event type(s) the reactor needs to observe. These events must already exist or be created as part of the slice.

## Step 2 — Create the reactor class

Place the reactor in the appropriate feature folder, co-located with the slice it belongs to.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyApp.Projects.Notifications;

/// <summary>
/// Sends a notification when a project is registered.
/// </summary>
/// <param name="notifications">The notification service.</param>
public class ProjectRegisteredNotifier(INotificationService notifications) : IReactor
{
    /// <summary>
    /// Reacts to <see cref="Registration.ProjectRegistered"/> events.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task ProjectRegistered(Registration.ProjectRegistered @event, EventContext context) =>
        await notifications.Notify($"Project '{@event.Name}' was registered.");
}
```

## Step 3 — Follow the critical rules

- **`IReactor` is a marker interface** — no methods to implement. Method dispatch is by first-parameter event type.
- **Method name** — can be anything descriptive. The name is for readability, not dispatch.
- **`EventContext`** — optional second parameter. Omit if event metadata is not needed.
- **Idempotent** — reactors may be called more than once for the same event. Design accordingly.
- **Use event data directly** — never query the read model back inside the reactor.
- **Trigger commands for further writes** — inject `ICommandPipeline` and execute a command. Never use `IEventLog` directly.

## Step 4 — Translation pattern (if applicable)

If the reactor adapts events from one slice by triggering commands in another, it is a **Translation** reactor:

```csharp
/// <summary>
/// Reacts to reservation events and decreases stock accordingly.
/// </summary>
/// <param name="stockKeeper">The stock keeper service.</param>
/// <param name="commandPipeline">The command pipeline.</param>
public class StockKeeping(IStockKeeper stockKeeper, ICommandPipeline commandPipeline) : IReactor
{
    /// <summary>
    /// Handles a <see cref="BookReserved"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task BookReserved(BookReserved @event, EventContext context) =>
        await commandPipeline.Execute(new DecreaseStock(@event.Isbn, await stockKeeper.GetStock(@event.Isbn)));
}
```

## Step 5 — Validate

Run `dotnet build`. Fix all errors before completing.

## When to use a Reactor vs a Projection

| Need | Use |
|------|-----|
| Populate a queryable read model from events | **Projection** (see `add-projection` skill) |
| Trigger side effects, send notifications, call external APIs | **Reactor** |
| Adapt events across slices by triggering commands | **Reactor** (Translation pattern) |
| Both populate a read model AND trigger side effects | **Both** — one projection + one reactor |
