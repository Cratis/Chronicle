```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ExternalReducerUserInvited(Guid UserId);

public record ExternalReducerUserTotals(int Count);

[EventStore("identity-service")]
[Reducer]
public class ExternalReducerUserTotalsReducer : IReducerFor<ExternalReducerUserTotals>
{
    public ExternalReducerUserTotals Invited(ExternalReducerUserInvited @event, ExternalReducerUserTotals? current, EventContext context) =>
        new((current?.Count ?? 0) + 1);
}
```
