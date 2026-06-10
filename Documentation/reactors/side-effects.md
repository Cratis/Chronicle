# Returning Side Effects from Reactor Handler Methods

Reactor handler methods can return side-effect events directly instead of taking a dependency on `IEventLog`. The framework automatically appends the returned events to the correct sequence after the handler completes.

> **Important**: If an append operation fails (constraint violation, concurrency violation, or error), a `ReactorAppendFailedException` is thrown and the reactor partition is marked as failed. The partition will be retried according to the observer retry policy.

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

## Multiple Side Effects

Return `IEnumerable<TEvent>` to append several events in one handler call:

```csharp
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

| Metadata | Priority |
|---|---|
| `EventSourceId` | `ICanProvideEventSourceId` → `eventContext.EventSourceId` |
| `EventStreamId` | `ICanProvideEventStreamId` → `[EventStreamId]` attribute → `null` |
| `EventStreamType` | `[EventStreamType]` attribute → `null` |
| `EventSourceType` | `[EventSourceType]` attribute → `null` |
| `Subject` | `ICanProvideSubject` → `null` |

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
| `IEnumerable<TEvent>` | `EventsResultHandler` — appends each event |
| `Task<IEnumerable<TEvent>>` | `EventsResultHandler` |
| `void` / `Task` | No side effects appended |

## Error Handling

When a reactor returns side-effect events, the framework checks the `AppendResult` of each append operation. If any append fails, a `ReactorAppendFailedException` is thrown containing:

- **Constraint violations**: Unique constraint violations, or other validation failures
- **Concurrency violations**: Version conflicts or concurrent modification errors
- **Errors**: Database connection errors or other infrastructure failures

The exception propagates to the observer infrastructure, which:
1. Marks the partition as failed
2. Records the full exception details and stack trace
3. Retries according to the observer retry policy
4. Quarantines the observer if the retry limit is exceeded

This ensures that append failures don't go unnoticed and the reactor partition can be recovered or debugged through the failed partitions API.

