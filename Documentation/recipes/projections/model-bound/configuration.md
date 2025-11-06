# Projection Configuration

Model-bound projections support class-level attributes to configure projection behavior, corresponding to the configuration methods available in fluent projections.

## Event Sequence Source

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

## Not Rewindable

Mark a projection as not rewindable (forward-only) using `[NotRewindable]`:

```csharp
[NotRewindable]
public record AuditLog(
    [Key]
    Guid Id,

    [SetFrom<AuditEvent>]
    string Message,

    [SetFrom<AuditEvent>]
    DateTimeOffset Timestamp);
```

This corresponds to the fluent API:

```csharp
projection.NotRewindable();
```

**When to use:**
- Audit logs or append-only data
- Projections that should never be replayed from scratch
- Time-sensitive data where replay would be incorrect

## Passive Projections

Mark a projection as passive (not actively observing) using `[Passive]`:

```csharp
[Passive]
public record HistoricalSnapshot(
    [Key]
    Guid Id,

    [SetFrom<SnapshotCreated>]
    string Data);
```

This corresponds to the fluent API:

```csharp
projection.Passive();
```

**When to use:**
- On-demand projections that are only rebuilt when explicitly requested
- Historical snapshots
- Resource-intensive projections that shouldn't run continuously

## Combined Configuration

You can combine multiple configuration attributes:

```csharp
[FromEventSequence("audit-log")]
[NotRewindable]
[Passive]
public record AuditSnapshot(
    [Key]
    Guid Id,

    [SetFrom<AuditEvent>]
    string EventType,

    [SetFrom<AuditEvent>]
    DateTimeOffset OccurredAt);
```

## Best Practices

1. **Event Sequence**: Only specify when you need to read from a non-default event sequence
2. **Not Rewindable**: Use for append-only scenarios where historical replay would be problematic
3. **Passive**: Use for expensive projections that are only needed occasionally
4. **Combination**: Be careful when combining - ensure the combination makes sense for your use case

## Comparison with Fluent API

| Attribute | Fluent Method | Default Value |
|-----------|--------------|---------------|
| `[FromEventSequence("id")]` | `.FromEventSequence(eventSequenceId)` | EventSequenceId.Log |
| `[NotRewindable]` | `.NotRewindable()` | Rewindable (true) |
| `[Passive]` | `.Passive()` | Active (true) |
