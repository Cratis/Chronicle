```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;

public static class TestingSeqAssertValidator
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var authorId = EventSourceId.New();

        await scenario.EventLog.Append(authorId, new TestingSeqAssertAuthorRegistered("Jane Smith"));

        await scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(0, author =>
            author.Name.ShouldEqual("Jane Smith"));
    }
}
```
