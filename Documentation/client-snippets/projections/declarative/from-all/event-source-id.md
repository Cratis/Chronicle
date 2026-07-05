```csharp title="Map event source id with FromAll"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AccountOpenedDeclarativeAll(string OwnerName);

public record AccountSummaryDeclarativeAll(
    string AccountId,
    string OwnerName);

public class AccountSummaryDeclarativeAllProjection : IProjectionFor<AccountSummaryDeclarativeAll>
{
    public void Define(IProjectionBuilderFor<AccountSummaryDeclarativeAll> builder) => builder
        .From<AccountOpenedDeclarativeAll>()
        .FromAll(_ => _
            .Set(m => m.AccountId)
            .ToEventSourceId());
}
```
