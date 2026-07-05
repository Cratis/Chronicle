```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
public record TestingAssertionsAuthorRegistered([property: Unique(name: "TestingAssertionsUniqueAuthorName")] string Name);

public static class TestingAssertionsHappyPath
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var authorId = EventSourceId.New();
        var result = await scenario.EventLog.Append(authorId, new TestingAssertionsAuthorRegistered("Jane Smith"));
        result.ShouldBeSuccessful();
    }
}
```
