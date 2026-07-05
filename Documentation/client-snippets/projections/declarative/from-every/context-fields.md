```csharp title="Map multiple context fields"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AccountTouchedDeclarativeEvery(string Reason);

public record AccountAuditDeclarativeEvery(
    DateTimeOffset LastUpdated,
    EventSequenceNumber LastEventSequence,
    string LastCorrelationId);

public class AccountAuditDeclarativeEveryProjection : IProjectionFor<AccountAuditDeclarativeEvery>
{
    public void Define(IProjectionBuilderFor<AccountAuditDeclarativeEvery> builder) => builder
        .From<AccountTouchedDeclarativeEvery>()
        .FromEvery(_ => _
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.LastEventSequence).ToEventContextProperty(c => c.SequenceNumber)
            .Set(m => m.LastCorrelationId).ToEventContextProperty(c => c.CorrelationId));
}
```
