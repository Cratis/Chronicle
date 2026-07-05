```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;
using Xunit;

public record TestSliceBookId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static TestSliceBookId New() => new(Guid.NewGuid());
}

[EventType]
public record TestSliceBookAdded(string Title, [property: Unique(name: "TestSliceUniqueIsbn")] string Isbn);

[EventType]
public record TestSliceBookBorrowed(string BorrowedBy);

public class when_adding_a_book : Specification, IDisposable
{
    EventScenario _scenario = null!;
    AppendResult _result = null!;

    void Establish() => _scenario = new EventScenario();

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            TestSliceBookId.New(),
            new TestSliceBookAdded("The Pragmatic Programmer", "978-0135957059"));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
```
