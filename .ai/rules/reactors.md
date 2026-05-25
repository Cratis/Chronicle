---
applyTo: "**/*.cs"
---

# Reactor Instructions

Reactors are the "if this then that" of event sourcing — they observe events and produce side effects. Unlike projections (which build state), reactors *do things*: send emails, trigger commands in other slices, call external APIs.

## IReactor — Marker Interface

`IReactor` is a **marker interface** with no methods to implement. Method dispatch is entirely by convention: the first parameter type of each public method determines which event it handles.

```csharp
public class ProjectRegisteredNotifier(INotificationService notifications) : IReactor
{
    /// <summary>
    /// Reacts to <see cref="ProjectRegistered"/> events by sending a notification.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task ProjectRegistered(ProjectRegistered @event, EventContext context) =>
        await notifications.Notify($"Project '{@event.Name}' was registered.");
}
```

## Method Signature

```csharp
public Task MethodName(TEvent @event, EventContext context)
```

- **First parameter** — the event type. This determines which events the method subscribes to.
- **Second parameter** — `EventContext` (optional). Omit if event metadata is not needed.
- **Return type** — `Task` (always async), or `Task<TResult>` to return side-effect events (see below).
- **Method name** — can be anything descriptive. The name is for readability, not dispatch.

## Returning Side-Effect Events

Instead of taking a dependency on `IEventLog`, reactor handler methods can return events that should be appended automatically. The default target is the event log; the EventSourceId defaults to the one from the triggering event.

### Return a single event

```csharp
public Task<SomeEvent> Handle(AnEvent @event, EventContext context) =>
    Task.FromResult(new SomeEvent(@event.Name));
```

### Return a collection of events

```csharp
public Task<IEnumerable<object>> Handle(AnEvent @event, EventContext context) =>
    Task.FromResult<IEnumerable<object>>([new SomeEvent(), new AnotherEvent()]);
```

### Return with full control via `ReactorSideEffect`

Use `ReactorSideEffect` to specify the EventSourceId, EventSequenceId, EventStreamType, EventStreamId, EventSourceType, or Subject:

```csharp
public Task<ReactorSideEffect> Handle(AnEvent @event, EventContext context) =>
    Task.FromResult(new ReactorSideEffect
    {
        Event = new SomeEvent(@event.Name),
        EventSourceId = EventSourceId.New(),             // optional — defaults to context EventSourceId
        EventSequenceId = EventSequenceId.Log,           // optional — defaults to event log
        EventStreamType = new EventStreamType("custom"), // optional
        EventSourceType = new EventSourceType("order"),  // optional
    });
```

### Return multiple side effects with metadata

```csharp
public Task<IEnumerable<ReactorSideEffect>> Handle(AnEvent @event, EventContext context) =>
    Task.FromResult<IEnumerable<ReactorSideEffect>>(
    [
        ReactorSideEffect.For(new SomeEvent()),
        new ReactorSideEffect { Event = new AnotherEvent(), EventSourceId = @event.RelatedId }
    ]);
```

## Critical Rules

1. **Idempotent** — Reactors may be called more than once for the same event (e.g. during replay or recovery). Design accordingly.
2. **Use event data directly** — Never query the read model back inside a reactor. The event contains all the information you need.
3. **Return events instead of injecting IEventLog** — If the reactor needs to produce new events, return them directly as `Task<TEvent>`, `Task<ReactorSideEffect>`, or a collection thereof. For commands in other slices, inject `ICommandPipeline` and execute a command. Avoid injecting `IEventLog` directly into a reactor.
4. **Single responsibility** — Each reactor class should have a focused purpose. Multiple handler methods in one reactor are fine if they serve the same automation concern.
5. **Failure behavior** — If a reactor throws, the failing event source partition pauses until the issue is resolved. Design for resilience.
6. **No state** — Reactors should be stateless. Inject dependencies via primary constructor, but do not store mutable state on the class.

## Slice Types That Use Reactors

| Slice type | Pattern |
|------------|---------|
| **Automation** | Reacts to events, makes decisions, triggers side effects |
| **Translation** | Adapts events from one slice/system by triggering commands in another |

### Automation Example

```csharp
public class ProjectRegisteredNotifier(INotificationService notifications) : IReactor
{
    /// <summary>
    /// Sends a notification when a project is registered.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task ProjectRegistered(ProjectRegistered @event, EventContext context) =>
        await notifications.Notify($"Project '{@event.Name}' was registered.");
}
```

### Translation Example

```csharp
public class StockKeeping(IStockKeeper stockKeeper, ICommandPipeline commandPipeline) : IReactor
{
    /// <summary>
    /// Reacts to a book reservation by decreasing stock.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task BookReserved(BookReserved @event, EventContext context) =>
        await commandPipeline.Execute(new DecreaseStock(@event.Isbn, await stockKeeper.GetStock(@event.Isbn)));
}
```

## Testing Reactors

Reactor specs follow the same BDD-style pattern as other specs. Use `IReactorInvoker` and `ReactorHandler` from the test infrastructure:

```csharp
// given/all_dependencies.cs
public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IReactorInvoker _reactorInvoker;
}

// given/a_reactor_handler.cs
public class a_reactor_handler : all_dependencies
{
    protected ReactorHandler _handler;
    void Establish() => _handler = new(_eventStore, _reactorInvoker);
}
```

See `specs.csharp.instructions.md` for full spec patterns.
