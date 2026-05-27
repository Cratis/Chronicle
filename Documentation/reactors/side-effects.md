# Returning Side Effects from Reactor Handler Methods

Reactor handler methods can return side-effect events directly instead of taking a dependency on `IEventLog`. The framework automatically appends the returned events to the correct sequence after the handler completes.

## Basic Usage

Return a single event directly from a handler method — synchronously or as a `Task<T>`:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class WarehouseReactor : IReactor
{
    // Synchronous — no async overhead when the result is already available
    public StockDecreased BookReserved(BookReserved @event, EventContext context) =>
        new StockDecreased(@event.Isbn, 1);

    // Asynchronous — use when you need to await something before producing the event
    public async Task<StockDecreased> BookReservedAsync(BookReserved @event, EventContext context)
    {
        var available = await FetchCurrentStockAsync(@event.Isbn);
        return new StockDecreased(@event.Isbn, available);
    }

    Task<int> FetchCurrentStockAsync(string isbn) => Task.FromResult(0);
}
```

The returned event is appended to the event log using the `EventSourceId` from the incoming `EventContext`. No `IEventLog` injection required.

## Controlling Where Events Are Appended

Return a `ReactorSideEffect` to override any of the append metadata per event:

```csharp
public ReactorSideEffect BookReserved(BookReserved @event, EventContext context) =>
    new ReactorSideEffect
    {
        Event = new StockDecreased(@event.Isbn, 1),
        EventSourceId = @event.WarehouseId,           // override the event source
        EventSequenceId = EventSequenceId.Log,        // target a specific sequence
    };
```

All fields on `ReactorSideEffect` are optional. Any that are left `null` fall back to reactor-level resolution (see below).

## Multiple Side Effects

Return `IEnumerable<ReactorSideEffect>` or `IEnumerable<TEvent>` to append several events in one handler call:

```csharp
public IEnumerable<ReactorSideEffect> BookReserved(BookReserved @event, EventContext context) =>
[
    ReactorSideEffect.For(new StockDecreased(@event.Isbn, 1)),
    new ReactorSideEffect { Event = new StockLow(@event.Isbn), EventSourceId = @event.WarehouseId },
];

// Or, if all events share the same metadata:
public IEnumerable<object> BookReserved(BookReserved @event, EventContext context) =>
[
    new StockDecreased(@event.Isbn, 1),
    new StockLow(@event.Isbn),
];
```

## Reactor-Level Metadata Resolution

Set metadata once on the reactor type so every returned event inherits it automatically.

### `ICanProvideEventSourceId`

Implement this interface to supply a custom `EventSourceId` for all side-effect events from this reactor:

```csharp
public class WarehouseReactor : IReactor, ICanProvideEventSourceId
{
    readonly string _warehouseId;

    public WarehouseReactor(string warehouseId) => _warehouseId = warehouseId;

    public EventSourceId GetEventSourceId() => _warehouseId;

    public StockDecreased BookReserved(BookReserved @event, EventContext context) =>
        new StockDecreased(@event.Isbn, 1);
}
```

### `ICanProvideSubject`

Implement this interface to attach a `Subject` (e.g. a user or principal) to appended events:

```csharp
public class OrderReactor : IReactor, ICanProvideSubject
{
    readonly string _userId;

    public OrderReactor(string userId) => _userId = userId;

    public Subject GetSubject() => new Subject(_userId);
}
```

### `ICanProvideEventStreamId`

Implement this interface to specify a runtime `EventStreamId`:

```csharp
public class TenantReactor : IReactor, ICanProvideEventStreamId
{
    readonly string _tenantId;

    public TenantReactor(string tenantId) => _tenantId = tenantId;

    public EventStreamId GetEventStreamId() => _tenantId;
}
```

### `[EventStreamType]` and `[EventSourceType]` Attributes

Apply these attributes to the reactor class for a compile-time stream or source type:

```csharp
[EventStreamType("warehouse")]
[EventSourceType("product")]
public class WarehouseReactor : IReactor
{
    public StockDecreased BookReserved(BookReserved @event, EventContext context) =>
        new StockDecreased(@event.Isbn, 1);
}
```

### Priority Order

When both per-event fields and reactor-level values are present, explicit values on `ReactorSideEffect` take highest priority:

| Metadata | Priority |
|---|---|
| `EventSourceId` | `ReactorSideEffect.EventSourceId` → `ICanProvideEventSourceId` → `eventContext.EventSourceId` |
| `EventStreamId` | `ReactorSideEffect.EventStreamId` → `ICanProvideEventStreamId` → `[EventStreamId]` attribute → `null` |
| `EventStreamType` | `ReactorSideEffect.EventStreamType` → `[EventStreamType]` attribute → `null` |
| `EventSourceType` | `ReactorSideEffect.EventSourceType` → `[EventSourceType]` attribute → `null` |
| `Subject` | `ReactorSideEffect.Subject` → `ICanProvideSubject` → `null` |

## Custom Return Type Handlers

Extend the pipeline by registering a custom `IReactorSideEffectHandler`:

```csharp
public class MyHandler : IReactorSideEffectHandler
{
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is MySpecialResult;

    public async Task Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        // process value
    }
}

// Register in DI
services.AddSingleton<IReactorSideEffectHandler, MyHandler>();
```

## Supported Return Types

| Return type | Handler invoked |
|---|---|
| `TEvent` | `EventResultHandler` — appends single event |
| `Task<TEvent>` | `EventResultHandler` — appends single event |
| `ReactorSideEffect` | `ReactorSideEffectResultHandler` — appends with full metadata control |
| `Task<ReactorSideEffect>` | `ReactorSideEffectResultHandler` |
| `IEnumerable<TEvent>` | `EventsResultHandler` — appends each event |
| `Task<IEnumerable<TEvent>>` | `EventsResultHandler` |
| `IEnumerable<ReactorSideEffect>` | `ReactorSideEffectsResultHandler` — appends each with metadata |
| `Task<IEnumerable<ReactorSideEffect>>` | `ReactorSideEffectsResultHandler` |
| `void` / `Task` | No side effects appended |
