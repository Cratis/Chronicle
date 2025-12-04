# Event Sequence Source

By default, projections read from the event log. Use `[FromEventSequence]` to specify a different event sequence:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEventSequence("custom-sequence")]
public record OrderSummary(
    [Key]
    Guid Id,

    [SetFrom<OrderPlaced>]
    decimal TotalAmount);
```

This corresponds to the fluent API:

```csharp
projection.FromEventSequence(new EventSequenceId("custom-sequence"));
```

## When to use

- When you need to read from a specific event sequence rather than the default event log
- For projections that should only consider events from a particular source or context
- When working with partitioned or specialized event streams

## Best practices

- Only specify when you need to read from a non-default event sequence
- Ensure the event sequence exists and contains the events your projection needs
- Consider the implications of switching event sequences on existing projections
