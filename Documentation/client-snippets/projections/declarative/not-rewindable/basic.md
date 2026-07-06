```csharp
using Cratis.Chronicle.Projections;

public class DecNotRewindableAuditLogProjection : IProjectionFor<DecNotRewindableAuditLogEntry>
{
    public void Define(IProjectionBuilderFor<DecNotRewindableAuditLogEntry> builder) => builder
        .NotRewindable()
        .AutoMap()
        .FromEvery(_ => _
            .Set(m => m.ProcessedAt).ToEventContextProperty(c => c.Occurred))
        .From<DecNotRewindableUserAction>(_ => _
            .Set(m => m.OccurredAt).ToEventContextProperty(c => c.Occurred));
}
```
