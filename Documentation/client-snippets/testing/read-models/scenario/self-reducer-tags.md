```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

[EventType]
[Tag("origin")]
public record ScenarioBalanceAdded(decimal Amount);

[FilterEventsByTag("accepted")]
[EventSourceType("Account")]
[EventStreamType("Payments")]
public record ScenarioSelfBalance(decimal Balance)
{
    public static ScenarioSelfBalance Reduce(ScenarioBalanceAdded @event, ScenarioSelfBalance? current) =>
        new((current?.Balance ?? 0m) + @event.Amount);
}

public static class SelfReducerTagScenario
{
    public static async Task Run()
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(ScenarioBalanceAdded)],
            Reducers = [typeof(ScenarioSelfBalance)]
        };
        var scenario = new ReadModelScenario<ScenarioSelfBalance>(null, new Defaults(artifacts));
        var source = scenario.Given.ForEventSource("account-1");
        await source.WithTags("accepted").OnStream("Account", "Payments")
            .Events(new ScenarioBalanceAdded(3m), new ScenarioBalanceAdded(5m));
        await source.OnStream("Account", "Payments").WithTags("rejected")
            .Events(new ScenarioBalanceAdded(100m));
        scenario.Instance!.Balance.ShouldEqual(8m);
    }
}
```
