# Event Processing

This guide explains how reactors discover methods, which signatures are supported, and how event context and errors are handled.

## Event Method Discovery

Reactors use convention-based method discovery. Chronicle finds and invokes public methods that:

- Accept the event type as the first parameter
- Optionally accept further dependency parameters (the `EventContext`, read models, or services)
- Return `void`, `Task`, `Task<T>`, or a synchronous side-effect type (see below)

Event parameter types must be marked with `[EventType]`.

## Supported Signatures

```csharp
// Synchronous with event only
void MethodName(TEvent @event);

// Synchronous with event and context
void MethodName(TEvent @event, EventContext context);

// Async with event only
Task MethodName(TEvent @event);

// Async with event and context
Task MethodName(TEvent @event, EventContext context);

// Async returning a side-effect event
Task<TResult> MethodName(TEvent @event);
Task<TResult> MethodName(TEvent @event, EventContext context);

// Synchronous returning a side-effect event (no Task overhead)
TResult MethodName(TEvent @event);
TResult MethodName(TEvent @event, EventContext context);
```

`TResult` can be an event type or `IEnumerable<TEvent>`.
See [Returning Side Effects](side-effects.md) for the full list of supported return types and metadata resolution.

## Event Context

The optional `EventContext` parameter provides metadata about the event, including the event source ID, sequence number, timestamps, and correlation identifiers.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class AuditReactor : IReactor
{
    public void AccountClosed(AccountClosed @event, EventContext context)
    {
        WriteAudit(@event.AccountId, context.Occurred, context.EventSourceId);
    }

    void WriteAudit(Guid accountId, DateTimeOffset occurred, EventSourceId eventSourceId) { }
}
```

## Taking Dependencies

Beyond the event and `EventContext`, a handler method can take additional parameters that Chronicle resolves when the method runs. Only the first parameter is fixed (it is the event that drives dispatch).

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class OrderProcessing(IShippingService shipping) : IReactor
{
    public async Task OrderPlaced(OrderPlaced @event, EventContext context, Order order, IPricingService pricing)
    {
        // 'order' is the read model, materialized strongly consistent for this event.
        // 'pricing' is resolved from the service provider.
        await shipping.Schedule(order, pricing.PriceFor(order));
    }
}
```

Each parameter after the event resolves as follows:

- An `EventContext` parameter receives the event context (its position does not matter).
- A read model — a type with a reducer or projection — is materialized on demand from that reducer or projection, making it **strongly consistent** when the reactor runs. Read models are resolved directly by Chronicle, never through the service provider.
- Any other type is resolved from the service provider.

### Resolving the read model key

By default the read model is materialized using the `EventSourceId` from the event context. When the key differs, implement `ICanResolveReadModelKey` on the reactor to return the `ReadModelKey` to use:

```csharp
public class OrderProcessing : IReactor, ICanResolveReadModelKey
{
    public ReadModelKey Resolve(object @event, EventContext context) =>
        ((OrderLineAdded)@event).OrderId;

    public Task OrderLineAdded(OrderLineAdded @event, Order order) => Task.CompletedTask;
}
```

The resolved key applies to every read model parameter across all of the reactor's handler methods.

## Event Source Isolation

Events are delivered per event source and in sequence order. Each reactor method is called for the specific event source that produced the event.

## Error Handling

If a reactor method throws an exception, the failing event source partition is marked as failed and processing for that partition is paused. Once the underlying issue is resolved, processing resumes from the last successful event.

## Related Rules

- [CHR0004](../code-analysis/CHR0004.md) - Reactor method signatures
- [CHR0005](../code-analysis/CHR0005.md) - Event parameters require `[EventType]`

