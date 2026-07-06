```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

public static class TestingScenarioGivenMultipleSources
{
    public static async Task Run()
    {
        var author1Id = EventSourceId.New();
        var author2Id = EventSourceId.New();
        var scenario = new EventScenario();

        await scenario.Given
            .ForEventSource(author1Id)
            .Events(new TestingScenarioAuthorRegistered("Jane Smith"));

        await scenario.Given
            .ForEventSource(author2Id)
            .Events(new TestingScenarioAuthorRegistered("John Doe"));
    }
}
```
