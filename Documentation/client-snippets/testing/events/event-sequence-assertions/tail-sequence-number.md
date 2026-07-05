```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

[EventType]
public record TestingSeqAssertAuthorRegistered(string Name);

[EventType]
public record TestingSeqAssertBookAdded(string Title);

public static class TestingSeqAssertTailSequenceNumber
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var authorId = EventSourceId.New();

        await scenario.EventLog.Append(authorId, new TestingSeqAssertAuthorRegistered("Jane Smith"));
        await scenario.EventLog.Append(authorId, new TestingSeqAssertBookAdded("Clean Code"));

        await scenario.EventLog.ShouldHaveTailSequenceNumber(1);
    }
}
```
