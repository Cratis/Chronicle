```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;

public static class TestingSeqAssertByEventSource
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var author1 = EventSourceId.New();
        var author2 = EventSourceId.New();

        await scenario.EventLog.Append(author1, new TestingSeqAssertAuthorRegistered("Jane Smith"));
        await scenario.EventLog.Append(author2, new TestingSeqAssertAuthorRegistered("John Doe"));

        // With sequence number and validator
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(0, author1, author =>
            author.Name.ShouldEqual("Jane Smith"));

        // Without sequence number — finds any matching event for the event source
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(author2, author =>
            author.Name.ShouldEqual("John Doe"));

        // With a predicate instead of a validator
        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(
            author1, author => author.Name == "Jane Smith");
    }
}
```
