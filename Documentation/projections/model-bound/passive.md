# Passive Projections

Mark a projection as passive (not actively observing) using `[Passive]`:

```csharp
[Passive]
public record HistoricalSnapshot(
    [Key]
    Guid Id,

    [SetFrom<SnapshotCreated>]
    string Data);
```

## When to use

- On-demand projections that are only rebuilt when explicitly requested
- Historical snapshots
- Resource-intensive projections that shouldn't run continuously

## Best practices

- Use for expensive projections that are only needed occasionally
- Consider the trade-off between resource usage and data freshness
- Ensure you have mechanisms to trigger rebuilds when needed
