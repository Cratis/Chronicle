```csharp title="Map the event source id"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AccountOpenedDeclarativeEvery(string OwnerName);

public record AccountSummaryDeclarativeEvery(
    string AccountId,
    string OwnerName);

public class AccountSummaryDeclarativeEveryProjection : IProjectionFor<AccountSummaryDeclarativeEvery>
{
    public void Define(IProjectionBuilderFor<AccountSummaryDeclarativeEvery> builder) => builder
        .From<AccountOpenedDeclarativeEvery>()
        .FromEvery(_ => _
            .Set(m => m.AccountId)
            .ToEventSourceId());
}
```
