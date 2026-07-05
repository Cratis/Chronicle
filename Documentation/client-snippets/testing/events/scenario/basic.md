```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
public record TestingScenarioAuthorRegistered(string Name);

[EventType]
public record TestingScenarioBookAdded(string Title);

public static class TestingScenarioBasic
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var result = await scenario.EventLog.Append(EventSourceId.New(), new TestingScenarioAuthorRegistered("John Doe"));
        result.ShouldBeSuccessful();
    }
}
```
