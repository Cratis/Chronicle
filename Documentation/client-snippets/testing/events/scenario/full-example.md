```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;
using Xunit;

public class when_adding_a_book_to_an_author : Specification
{
    readonly EventSourceId _authorId = EventSourceId.New();
    readonly EventScenario _scenario = new();
    AppendResult _result = default!;

    Task Establish() =>
        _scenario.Given
            .ForEventSource(_authorId)
            .Events(new TestingScenarioAuthorRegistered("Jane Smith"));

    async Task Because() =>
        _result = await _scenario.EventLog.Append(_authorId, new TestingScenarioBookAdded("Clean Code"));

    [Fact] void should_append_successfully() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_appended_book_added() =>
        _scenario.EventLog.ShouldHaveAppendedEvent<TestingScenarioBookAdded>(new EventSequenceNumber(1));
}
```
