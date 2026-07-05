```csharp
[EventType]
public record DecConstantKeyUserRegistered(string Name);

[EventType]
public record DecConstantKeyUserLoggedIn;

[EventType]
public record DecConstantKeyUserLoggedOut;

public record DecConstantKeySiteStatistics(
    int TotalUsers,
    int ActiveSessions);

public class DecConstantKeySiteStatisticsProjection : IProjectionFor<DecConstantKeySiteStatistics>
{
    public void Define(IProjectionBuilderFor<DecConstantKeySiteStatistics> builder) => builder
        .From<DecConstantKeyUserRegistered>(_ => _
            .UsingConstantKey("site")
            .Count(m => m.TotalUsers))
        .From<DecConstantKeyUserLoggedIn>(_ => _
            .UsingConstantKey("site")
            .Increment(m => m.ActiveSessions))
        .From<DecConstantKeyUserLoggedOut>(_ => _
            .UsingConstantKey("site")
            .Decrement(m => m.ActiveSessions));
}
```
