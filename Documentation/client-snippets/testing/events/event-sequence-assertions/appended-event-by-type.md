```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

public static class TestingSeqAssertAppendedEventByType
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var authorId = EventSourceId.New();

        await scenario.EventLog.Append(authorId, new TestingSeqAssertAuthorRegistered("Jane Smith"));
        await scenario.EventLog.Append(authorId, new TestingSeqAssertBookAdded("Clean Code"));

        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>();
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertBookAdded>();
    }
}
```
