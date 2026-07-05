```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;

public static class TestingSeqAssertPredicate
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var authorId = EventSourceId.New();

        await scenario.EventLog.Append(authorId, new TestingSeqAssertAuthorRegistered("Jane Smith"));

        // Without sequence number — finds any matching event
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(
            author => author.Name == "Jane Smith");

        // At a specific sequence number
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(0,
            author => author.Name == "Jane Smith");
    }
}
```
