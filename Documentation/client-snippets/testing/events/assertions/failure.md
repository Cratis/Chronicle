```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

public static class TestingAssertionsFailure
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var authorId = EventSourceId.New();
        var newAuthorId = EventSourceId.New();

        await scenario.Given
            .ForEventSource(authorId)
            .Events(new TestingAssertionsAuthorRegistered("Jane Smith"));

        var result = await scenario.EventLog.Append(newAuthorId, new TestingAssertionsAuthorRegistered("Jane Smith"));
        result.ShouldBeFailed();
    }
}
```
