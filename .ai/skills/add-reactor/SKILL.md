---
name: add-reactor
description: Use this skill when asked to add a Chronicle reactor (automation or translation) to a Cratis-based project. Reactors observe events and produce side effects.
---

Add a Chronicle **reactor** that triggers automation or translation logic in response to events.

> **Always read the [reactors.md](../../rules/reactors.md) rule first.** It is the source of truth for reactor conventions, rules, and patterns.

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
- **Return events instead of injecting IEventLog** — to produce new events, return them directly as `Task<TEvent>`, `Task<EventForEventSourceId>`, or a collection thereof. For commands in other slices, inject `ICommandPipeline`. Avoid injecting `IEventLog` directly.

## Step 3b — Return side-effect events (alternative to IEventLog)

Return events directly from a handler method instead of calling `IEventLog.Append`. This keeps reactors free of direct event-store dependencies.

```csharp
// Return a single event — uses EventSourceId from incoming event, appends to EventLog
public Task<StockDecreased> BookReserved(BookReserved @event, EventContext context) =>
    Task.FromResult(new StockDecreased(@event.Isbn, 1));

// Return multiple events
public Task<IEnumerable<object>> BookReserved(BookReserved @event, EventContext context) =>
    Task.FromResult<IEnumerable<object>>([new StockDecreased(@event.Isbn, 1), new StockLow(@event.Isbn)]);

// Target a specific event source id with explicit metadata
// (EventSourceId, EventStreamType, EventStreamId, EventSourceType, Subject, Occurred, Causation)
public Task<EventForEventSourceId> BookReserved(BookReserved @event, EventContext context) =>
    Task.FromResult(new EventForEventSourceId(@event.WarehouseId, new StockDecreased(@event.Isbn, 1))
    {
        EventStreamType = new("warehouse"),   // optional — others default sensibly
    });

// Target multiple event source ids in one transaction
public Task<IEnumerable<EventForEventSourceId>> BookReserved(BookReserved @event, EventContext context) =>
    Task.FromResult<IEnumerable<EventForEventSourceId>>(
    [
        new(@event.WarehouseId, new StockDecreased(@event.Isbn, 1)),
        new(@event.Isbn, new StockLow(@event.Isbn)),
    ]);
```

For **bare event** returns, the append-metadata is resolved from the reactor itself: `[EventStreamType]`, `[EventSourceType]`, `[EventStreamId]` attributes and the `ICanProvideEventSourceId`, `ICanProvideEventStreamId`, `ICanProvideSubject` interfaces. Return an `EventForEventSourceId` only when you need explicit per-event control or to target several event source ids at once.

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

## Step 5 — Filter by appended event metadata (optional)

If the reactor should only observe a subset of appended events, decorate it with filter attributes that match the metadata used when appending:

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[FilterEventsByTag("priority")]
[EventSourceType("order")]
[EventStreamType("fulfillment")]
public class PriorityFulfillmentReactor : IReactor
{
    public Task OrderPlaced(OrderPlaced @event, EventContext context) => Task.CompletedTask;
}
```

These attributes correlate directly to the append call:

```csharp
await eventLog.Append(
    EventSourceId.New(),
    new OrderPlaced(42m),
    eventStreamType: "fulfillment",
    eventSourceType: "order",
    tags: ["priority"]);
```

- `[FilterEventsByTag]` filters by event tags
- `[EventSourceType]` filters by the appended event source type
- `[EventStreamType]` filters by the appended event stream type
- `[Tag]` and `[Tags]` still label the reactor itself; they do not filter events

For fuller examples, see `Documentation/events/filtering/`.

## Step 6 — Validate

Run `dotnet build`. Fix all errors before completing.

## When to use a Reactor vs a Projection

| Need | Use |
|------|-----|
| Populate a queryable read model from events | **Projection** (see `add-projection` skill) |
| Trigger side effects, send notifications, call external APIs | **Reactor** |
| Adapt events across slices by triggering commands | **Reactor** (Translation pattern) |
| Both populate a read model AND trigger side effects | **Both** — one projection + one reactor |
