```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;
using Xunit;

[EventType]
public record TestingSeqAssertLibraryCreated(string Name);

public record TestingSeqAssertAuthorId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static TestingSeqAssertAuthorId New() => new(Guid.NewGuid());
}

public class when_registering_an_author_and_adding_a_book
{
    readonly EventScenario _scenario = new();

    [Fact]
    public async Task should_append_both_events_in_order()
    {
        var authorId = TestingSeqAssertAuthorId.New();

        await _scenario.Given
            .ForEventSource(authorId)
            .Events(new TestingSeqAssertLibraryCreated("Main Library"));

        await _scenario.EventLog.Append(authorId, new TestingSeqAssertAuthorRegistered("Jane Smith"));
        await _scenario.EventLog.Append(authorId, new TestingSeqAssertBookAdded("Clean Code"));

        // Tail includes the seeded event (0) plus the two appended events (1, 2)
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(2);

        await _scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertAuthorRegistered>(1, author =>
            author.Name.ShouldEqual("Jane Smith"));
        await _scenario.EventLog.ShouldHaveAppendedEvent<TestingSeqAssertBookAdded>(2, book =>
            book.Title.ShouldEqual("Clean Code"));
    }
}
```
