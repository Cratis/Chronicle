```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecEventContextUserAction(string UserId, string ActionType);

public class DecEventContextAuditTrailProjection : IProjectionFor<DecEventContextAuditEntry>
{
    public void Define(IProjectionBuilderFor<DecEventContextAuditEntry> builder) => builder
        .AutoMap()
        .From<DecEventContextUserAction>(_ => _
            .Set(m => m.EventId).ToEventContextProperty(c => c.SequenceNumber)
            .Set(m => m.OccurredAt).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.CorrelationId).ToEventContextProperty(c => c.CorrelationId));
}
```
