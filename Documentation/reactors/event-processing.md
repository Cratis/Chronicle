# Event Processing

This guide explains how reactors discover methods, which signatures are supported, and how event context and errors are handled.

## Event Method Discovery

Reactors use convention-based method discovery. Chronicle finds and invokes public methods that:

- Accept the event type as the first parameter
- Optionally accept `EventContext` as the second parameter
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

// Async returning a side-effect event or ReactorSideEffect
Task<TResult> MethodName(TEvent @event);
Task<TResult> MethodName(TEvent @event, EventContext context);

// Synchronous returning a side-effect event or ReactorSideEffect (no Task overhead)
TResult MethodName(TEvent @event);
TResult MethodName(TEvent @event, EventContext context);
```

`TResult` can be an event type, `ReactorSideEffect`, `IEnumerable<TEvent>`, or `IEnumerable<ReactorSideEffect>`.
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

## Event Source Isolation

Events are delivered per event source and in sequence order. Each reactor method is called for the specific event source that produced the event.

## Error Handling

If a reactor method throws an exception, the failing event source partition is marked as failed and processing for that partition is paused. Once the underlying issue is resolved, processing resumes from the last successful event.

## Related Rules

- [CHR0004](../clients/dotnet/code-analysis/CHR0004.md) - Reactor method signatures
- [CHR0005](../clients/dotnet/code-analysis/CHR0005.md) - Event parameters require `[EventType]`

