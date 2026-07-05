```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

public static class TestingScenarioGiven
{
    public static async Task Run()
    {
        var authorId = EventSourceId.New();
        var scenario = new EventScenario();

        await scenario.Given
            .ForEventSource(authorId)
            .Events(new TestingScenarioAuthorRegistered("John Doe"), new TestingScenarioBookAdded("Clean Code"));

        var result = await scenario.EventLog.Append(authorId, new TestingScenarioBookAdded("The Pragmatic Programmer"));
        result.ShouldBeSuccessful();
    }
}
```
