```csharp title="Track audit metadata from every event"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record AuditableInventoryChangedForEvery(string Reason);

[FromEvent<AuditableInventoryChangedForEvery>]
public record AuditableInventoryStatusFromEvery(
    [Key] Guid Id,
    [FromEvery(contextProperty: nameof(EventContext.Occurred))]
    DateTimeOffset LastModified,
    [FromEvery(contextProperty: nameof(EventContext.SequenceNumber))]
    EventSequenceNumber LastEventSequence,
    [FromEvery(contextProperty: nameof(EventContext.CorrelationId))]
    string LastCorrelationId);
```
