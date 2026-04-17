---
agent: agent
description: Add a Chronicle reactor (automation or translation) that reacts to events and triggers side effects.
---

# Add a Reactor

I need to add a **Chronicle reactor** that observes events and produces side effects (automation, notifications, translations between slices).

> **Before starting:** Read `.github/instructions/reactors.instructions.md`.

## Inputs

- **Events to react to** — list the event types (e.g. `ProjectRegistered`, `BookReserved`)
- **Purpose** — describe the side-effect or automation to perform
- **Pattern** — `Automation` (reacts to events, triggers side effects) or `Translation` (adapts events across slices by triggering commands)

## Reactor rules (mandatory)

```csharp
public class AutomationName(IDependency dependency) : IReactor
{
    /// <summary>
    /// Handles <see cref="SomeEvent"/> events.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public Task HandleSomeEvent(SomeEvent @event, EventContext context) =>
        dependency.DoSomethingWith(@event);
}
```

**Critical rules:**
- `IReactor` is a **marker interface** — no methods to implement
- Event dispatch is by first-parameter type; method name can be anything descriptive
- `EventContext` is optional — omit if event metadata is not needed
- Reactors MUST be idempotent — they may be called more than once for the same event
- Do not query the read model inside the reactor — use the event data directly
- To produce new events, inject `ICommandPipeline` and execute a command — never use `IEventLog` directly

## Translation pattern

If the reactor adapts events by triggering commands in another slice:

```csharp
public class StockKeeping(IStockKeeper stockKeeper, ICommandPipeline commandPipeline) : IReactor
{
    /// <summary>
    /// Decreases stock when a book is reserved.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task BookReserved(BookReserved @event, EventContext context) =>
        await commandPipeline.Execute(new DecreaseStock(@event.Isbn, await stockKeeper.GetStock(@event.Isbn)));
}
```

## After creating the file

Run `dotnet build` and fix all errors before completing.
