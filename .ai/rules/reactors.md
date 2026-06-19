---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
profile: application
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
- **Second parameter** — `EventContext` (optional). Omit if event metadata is not needed. A reactor method takes no more than two parameters.
- **Return type** — `Task` or `void`, or a side-effect type (`TEvent`, `ReactorSideEffect`, or a collection of either) returned directly (sync) or wrapped in `Task<...>` (async). Prefer `Task`/async for real side effects, but synchronous returns are fully supported — there is no "always async" requirement.
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

### Target a specific event source id via `EventForEventSourceId`

Return an `EventForEventSourceId` to append to an explicit `EventSourceId` — for example a different entity than the one that triggered the reactor. It also carries the `EventStreamType`, `EventStreamId`, `EventSourceType`, `Subject`, `Occurred` time, `Tags` and `Causation` per event:

```csharp
public Task<EventForEventSourceId> Handle(AnEvent @event, EventContext context) =>
    Task.FromResult(new EventForEventSourceId(@event.RelatedId, new SomeEvent(@event.Name))
    {
        EventStreamType = new EventStreamType("custom"), // optional
        EventSourceType = new EventSourceType("order"),  // optional
        Subject = new Subject(@event.RelatedId),         // optional
    });
```

### Return events for multiple event source ids

Return `IEnumerable<EventForEventSourceId>` to append to several event source ids in one transaction:

```csharp
public Task<IEnumerable<EventForEventSourceId>> Handle(AnEvent @event, EventContext context) =>
    Task.FromResult<IEnumerable<EventForEventSourceId>>(
    [
        new(@event.RelatedId, new SomeEvent()),
        new(@event.OtherId, new AnotherEvent())
    ]);
```

> Reactor-level metadata (`ICanProvideEventSourceId`, `ICanProvideSubject`, `[EventStreamType]`, …) applies to bare `TEvent` returns. An `EventForEventSourceId` is self-describing, so its own values are used instead. You can mix bare events and `EventForEventSourceId` in a single `IEnumerable<object>` return — each is appended with its respective metadata, all in one transaction.

### Cross-stream via `EventForEventSourceId`

To append a side-effect event to a **different** event source, return `EventForEventSourceId(id, @event)` (single or `IEnumerable<EventForEventSourceId>`) — the same cross-stream wrapper a command `Handle()` uses. Reach for `ReactorSideEffect` instead when you also need to set the `EventSequenceId`, stream type, source type, or `Subject`.

```csharp
public Task<IEnumerable<EventForEventSourceId>> Handle(AnEvent @event, EventContext context) =>
    Task.FromResult<IEnumerable<EventForEventSourceId>>(
    [
        new EventForEventSourceId(@event.RelatedId, new SomeEvent())
    ]);
```

> **Chronicle version note:** reactor side-effect handling of `EventForEventSourceId` wrappers ships in an upcoming Chronicle release. On earlier versions, target another event source with `ReactorSideEffect { EventSourceId = … }` instead.

## External event stores (outbox / inbox)

Cross-service events route through Chronicle's outbox/inbox rather than a shared log:

- The producing service appends its **public contract event** to `EventSequenceId.Outbox`.
- The consuming observer listens to the implicit inbox sequence for the source store (`inbox-<source-store>`).
- Chronicle creates/reuses the outbox→inbox subscription from the observer's `[EventStore]` metadata, or from `[EventStore]` on the event type/assembly. Put `[assembly: EventStore("<source-store>")]` in the contracts project when every event there originates from one service; use observer-level `[EventStore("<source-store>")]` to override.
- An observer-level `[EventStore]` **cannot** be combined with `[EventSequence]`, `[EventLog]`, or `Reactor(eventSequence: ...)`.

## Confirming downstream completion — `WaitForCompletion`

The default is fire-and-forget. When a caller's correctness depends on all observers (projections, reducers, reactors) having processed an appended event before returning — integration tests, synchronous webhook surfaces — call `WaitForCompletion` (from `Cratis.Chronicle.Observation`) on the `AppendResult`/`AppendManyResult`. It returns an `AppendResultWaitForCompletionResult` carrying `IsSuccess` and `FailedPartitions` (the full set across all affected observers, not just the first to fail). Reach for it deliberately — not as a default.

## Critical Rules

1. **Idempotent** — Reactors may be called more than once for the same event (e.g. during replay or recovery). Design accordingly. For a side effect that must **not** repeat on replay (emails, payments, external writes), mark the handler method `[OnceOnly]` so Chronicle fires it a single time per event source.
2. **Use event data directly** — Never query the read model back inside a reactor. The event contains all the information you need.
3. **Return events instead of injecting IEventLog** — If the reactor needs to produce new events, return them directly as `Task<TEvent>`, `Task<EventForEventSourceId>`, or a collection thereof. For commands in other slices, inject `ICommandPipeline` and execute a command. Avoid injecting `IEventLog` directly into a reactor.
4. **Single responsibility** — Each reactor class should have a focused purpose. Multiple handler methods in one reactor are fine if they serve the same automation concern.
5. **Failure behavior** — If a reactor throws, *or* a returned side-effect event fails to append (constraint violation, concurrency violation, or error), the failing event-source partition pauses until the issue is resolved. Repeated failures can **quarantine** the observer: once `QuarantineOnFailedPartitionCount`/`QuarantineOnFailedPartitionPercentage` (under `Observers`) is crossed, the observer enters the `Quarantined` state — reminders cancelled, retries stopped, automatic recovery suppressed. **A quarantined observer does NOT auto-resume on reconnect** — an operator must call `ClearObserverQuarantine()`. A periodic watchdog (default 60s, `WatchdogInterval`) re-routes stuck/dead observers but does not rescue quarantined ones. Design for resilience.
6. **Don't throw to validate malformed inbound events** — reactors are not data-quality validators; invalid payloads must be rejected at the command/append site. When a malformed cross-service fact reaches a consumer, throwing just to reject it pauses the partition and can quarantine the whole observer. Instead append a clear failure/dead-letter event (e.g. `ProvisioningFailed`) or surface it via the operational failure path, and skip partial side effects.
7. **No state** — Reactors should be stateless. Inject dependencies via primary constructor, but do not store mutable state on the class.

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

Use `ReactorScenario<TReactor>` (from `Cratis.Chronicle.Testing.Reactors`) — construct it with an `IServiceProvider` of NSubstitute mocks, fire events through `Given`, and assert on the mocks:

```csharp
void Establish()
{
    _notifications = Substitute.For<INotificationService>();
    _scenario = new(new ServiceCollection().AddSingleton(_notifications).BuildServiceProvider());
}

async Task Because() => await _scenario.Given.ForEventSource(_id).Events(new ProjectRegistered("Acme"));

[Fact] async Task should_notify() => await _notifications.Received(1).Notify("Project 'Acme' was registered.");
```

For reactors that return side-effect events, assert the resulting appends through the scenario's event store; for non-event side effects, assert on the mocked services (as above). See [specs.scenarios.csharp.md](./specs.scenarios.csharp.md) for the full `*Scenario` family.
