```csharp title="Map context fields with FromAll"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AccountTouchedDeclarativeAll(string Reason);

public record AccountAuditDeclarativeAll(
    DateTimeOffset LastUpdated,
    EventSequenceNumber LastEventSequence,
    string LastCorrelationId);

public class AccountAuditDeclarativeAllProjection : IProjectionFor<AccountAuditDeclarativeAll>
{
    public void Define(IProjectionBuilderFor<AccountAuditDeclarativeAll> builder) => builder
        .From<AccountTouchedDeclarativeAll>()
        .FromAll(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.LastEventSequence).ToEventContextProperty(c => c.SequenceNumber)
            .Set(m => m.LastCorrelationId).ToEventContextProperty(c => c.CorrelationId));
}
```
