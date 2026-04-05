# Event Sequence Source

By default, model-bound projections read from the event log. Use `[EventSequence]` to specify a different event sequence:

```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;

[EventSequence("custom-sequence")]
public record OrderSummary(
    [Key]
    Guid Id,

    [SetFrom<OrderPlaced>]
    decimal TotalAmount);
```

This corresponds to the fluent API for declarative projections:

```csharp
projection.FromEventSequence(new EventSequenceId("custom-sequence"));
```

## Convenience: `[EventLog]`

Use `[EventLog]` when you want to be explicit that the projection reads from the default event log, even when other event types in the assembly carry a `[EventStore]` attribute:

```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;

[EventLog]
public record LocalSnapshot(
    [Key]
    Guid Id,

    [SetFrom<LocalEvent>]
    string Data);
```

## Automatic Inbox Routing

When the event types used in a model-bound projection carry a `[EventStore]` attribute pointing to a **different** event store than the one the projection is registered in, Chronicle automatically routes the projection to the corresponding inbox sequence — no `[EventSequence]` annotation needed.

When the `[EventStore]` attribute points to the **same** event store as the current one, Chronicle routes to the event log instead.

## When to use `[EventSequence]` explicitly

- When you need to read from a specific sequence other than the inferred one
- To suppress automatic inbox routing for a projection that handles foreign events
- For projections that target a specialized or partitioned event stream

> **Note:** The old `[FromEventSequence]` attribute has been removed. Use `[EventSequence]` instead.
