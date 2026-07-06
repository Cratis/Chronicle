```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecFunctionsUserLoggedIn(string Username);

[EventType]
public record DecFunctionsUserPerformedAction(string Username, string ActionType);

public record DecFunctionsUserActivity(
    string Username,
    int LoginCount,
    int ActionCount);

public class DecFunctionsUserActivityProjection : IProjectionFor<DecFunctionsUserActivity>
{
    public void Define(IProjectionBuilderFor<DecFunctionsUserActivity> builder) => builder
        .AutoMap()
        .From<DecFunctionsUserLoggedIn>(_ => _
            .Count(m => m.LoginCount))
        .From<DecFunctionsUserPerformedAction>(_ => _
            .Count(m => m.ActionCount));
}
```
