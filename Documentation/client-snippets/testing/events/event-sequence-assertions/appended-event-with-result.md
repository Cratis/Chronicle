```csharp
using System.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;

public static class TestingSeqAssertAppendedEventWithResult
{
    public static async Task Run()
    {
        var scenario = new EventScenario();
        var authorId = EventSourceId.New();

        var result = await scenario.EventLog.Append(authorId, new TestingSeqAssertAuthorRegistered("Jane Smith"));
        var appendedEvents = await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, authorId);
        var collected = new AppendedEventWithResult(appendedEvents.Last(), result);

        collected.ShouldBeSuccessful();
        collected.ShouldHaveEvent<TestingSeqAssertAuthorRegistered>(author =>
            author.Name.ShouldEqual("Jane Smith"));
        collected.ShouldBeForEventSource(authorId);
    }
}
```
