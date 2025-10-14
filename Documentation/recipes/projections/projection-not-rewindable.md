# Projection with NotRewindable

The `NotRewindable()` method marks a projection as forward-only, meaning it cannot be replayed or rewound. This is useful for projections that maintain state that should not be recreated from scratch or for performance-critical scenarios where replay is not needed.

## Defining a non-rewindable projection

Use `NotRewindable()` to mark a projection as forward-only:

```csharp
using Cratis.Chronicle.Projections;

public class AuditLogProjection : IProjectionFor<AuditLogEntry>
{
    public void Define(IProjectionBuilderFor<AuditLogEntry> builder) => builder
        .NotRewindable()
        .FromEvery(_ => _
            .Set(m => m.ProcessedAt).To(_ => DateTimeOffset.UtcNow))
        .From<UserAction>(_ => _
            .Set(m => m.UserId).To(e => e.UserId)
            .Set(m => m.Action).To(e => e.ActionType)
            .Set(m => m.Details).To(e => e.Details)
            .Set(m => m.OccurredAt).ToEventContextProperty(c => c.Occurred));
}
```

This projection:

- Cannot be replayed or reset
- Processes events only as they arrive in real-time
- Maintains forward-only state progression
- Is optimized for performance and append-only scenarios

## Read model definition

The read model can include timestamp and audit information:

```csharp
public record AuditLogEntry(
    string UserId,
    string Action,
    string Details,
    DateTimeOffset OccurredAt,
    DateTimeOffset ProcessedAt,
    long SequenceNumber);
```

## Event definitions

Events should be designed for forward-only processing:

```csharp
[EventType]
public record UserAction(
    string UserId,
    string ActionType,
    string Details);

[EventType]
public record SystemEvent(
    string ComponentName,
    string EventType,
    string Data);
```

## How it works

When a projection is marked as `NotRewindable()`:

1. **No replay capability**: The projection cannot be reset and rebuilt from the beginning
2. **Forward-only processing**: Events are processed only as they arrive in chronological order
3. **Performance optimization**: Chronicle skips replay infrastructure and optimizations
4. **State preservation**: Existing projection state is preserved and cannot be recreated
5. **Error handling**: Failed events may require manual intervention since replay isn't available

## Use cases for non-rewindable projections

### Audit and logging projections

Perfect for audit logs where you want to preserve the exact timing and sequence:

```csharp
public class SecurityAuditProjection : IProjectionFor<SecurityAuditEntry>
{
    public void Define(IProjectionBuilderFor<SecurityAuditEntry> builder) => builder
        .NotRewindable()
        .FromEvery(_ => _
            .Set(m => m.AuditedAt).To(_ => DateTimeOffset.UtcNow)
            .Set(m => m.ServerName).To(_ => Environment.MachineName))
        .From<UserLoginAttempt>(_ => _
            .Set(m => m.UserId).To(e => e.UserId)
            .Set(m => m.Success).To(e => e.Success)
            .Set(m => m.IpAddress).To(e => e.IpAddress))
        .From<PermissionChange>(_ => _
            .Set(m => m.UserId).To(e => e.UserId)
            .Set(m => m.Permission).To(e => e.Permission)
            .Set(m => m.Granted).To(e => e.Granted));
}
```

### Performance monitoring

For real-time performance metrics that don't need historical accuracy:

```csharp
public class PerformanceMetricProjection : IProjectionFor<PerformanceMetric>
{
    public void Define(IProjectionBuilderFor<PerformanceMetric> builder) => builder
        .NotRewindable()
        .From<ApiRequestCompleted>(_ => _
            .Set(m => m.Endpoint).To(e => e.Endpoint)
            .Set(m => m.ResponseTime).To(e => e.ResponseTime)
            .Set(m => m.StatusCode).To(e => e.StatusCode)
            .Set(m => m.Timestamp).ToEventContextProperty(c => c.Occurred));
}
```

### Financial transactions

For append-only financial records where replay could cause confusion:

```csharp
public class TransactionLedgerProjection : IProjectionFor<LedgerEntry>
{
    public void Define(IProjectionBuilderFor<LedgerEntry> builder) => builder
        .NotRewindable()
        .FromEvery(_ => _
            .Set(m => m.RecordedAt).To(_ => DateTimeOffset.UtcNow))
        .From<PaymentProcessed>(_ => _
            .Set(m => m.Amount).To(e => e.Amount)
            .Set(m => m.Currency).To(e => e.Currency)
            .Set(m => m.AccountId).To(e => e.AccountId)
            .Set(m => m.TransactionType).To(_ => "PAYMENT"));
}
```

## When to use NotRewindable

Use `NotRewindable()` when:

- **Audit requirements**: You need to preserve exact processing timestamps and sequence
- **Performance critical**: The projection handles high-volume events and replay would be expensive
- **Append-only data**: The projection represents data that should never be regenerated
- **Real-time processing**: Current processing time is part of the business logic
- **External integrations**: The projection triggers external actions that shouldn't be repeated
- **Compliance**: Regulatory requirements prevent data recreation or replay

## When NOT to use NotRewindable

Avoid `NotRewindable()` when:

- **Business logic changes**: You might need to replay events with updated logic
- **Bug fixes**: Errors in projection logic need to be corrected by replay
- **Data migration**: You need to rebuild projections with new schemas
- **Testing**: Development and testing scenarios benefit from replay capability
- **Recovery**: System failures might require rebuilding projection state

## Combining with other features

NotRewindable can be combined with other projection features:

```csharp
public class RealTimeOrderStatusProjection : IProjectionFor<OrderStatus>
{
    public void Define(IProjectionBuilderFor<OrderStatus> builder) => builder
        .NotRewindable()
        .FromEventSequence("order-processing")
        .Passive()
        .FromEvery(_ => _
            .Set(m => m.LastUpdatedAt).To(_ => DateTimeOffset.UtcNow)
            .Set(m => m.ProcessingNode).To(_ => Environment.MachineName))
        .From<OrderReceived>(_ => _
            .Set(m => m.OrderId).To(e => e.OrderId)
            .Set(m => m.Status).To(_ => "RECEIVED"))
        .From<OrderProcessing>(_ => _
            .Set(m => m.Status).To(_ => "PROCESSING"))
        .From<OrderCompleted>(_ => _
            .Set(m => m.Status).To(_ => "COMPLETED"));
}
```

This combines non-rewindable behavior with event sequence specification, passive mode, and real-time timestamp tracking.

## Error handling considerations

With non-rewindable projections:

- **Failed events**: Must be handled carefully since replay isn't available
- **Dead letter queues**: Consider implementing for failed event processing
- **Manual intervention**: May be required to fix projection state
- **Monitoring**: Implement comprehensive monitoring and alerting
- **Backup strategies**: Consider point-in-time snapshots for recovery

## Performance benefits

Non-rewindable projections offer several performance advantages:

- **Reduced memory usage**: No need to store replay state or checkpoints
- **Faster startup**: No replay phase during application startup
- **Lower CPU usage**: Eliminates replay processing overhead
- **Simplified infrastructure**: Fewer moving parts in the projection system
- **Better throughput**: All processing power focused on real-time events
