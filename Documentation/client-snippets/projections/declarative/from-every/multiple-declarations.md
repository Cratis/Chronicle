```csharp title="Multiple FromEvery declarations"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record UserChangedDeclarativeEveryMultiple(string Name);

public record UserAuditDeclarativeEveryMultiple(
    string Name,
    DateTimeOffset LastUpdated,
    string ModifiedBy);

public class UserAuditDeclarativeEveryMultipleProjection : IProjectionFor<UserAuditDeclarativeEveryMultiple>
{
    public void Define(IProjectionBuilderFor<UserAuditDeclarativeEveryMultiple> builder) => builder
        .From<UserChangedDeclarativeEveryMultiple>()
        .FromEvery(_ => _
            .Set(m => m.LastUpdated)
            .ToEventContextProperty(c => c.Occurred))
        .FromEvery(_ => _
            .Set(m => m.ModifiedBy)
            .ToEventContextProperty(c => c.CausedBy));
}
```
