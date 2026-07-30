```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Xunit;

[EventType]
public record Chr0039AuthorRegisteredFixed(string Name);

public class Chr0039WhenRegisteringAnAuthorFixed
{
    readonly IEventLog _eventLog = default!;

    // Declaring the fact 'async Task' and awaiting the assertion makes the exception it throws
    // observable, so the assertion can actually fail.
    [Fact]
    async Task should_have_appended_the_event() =>
        await _eventLog.ShouldHaveAppendedEvent<Chr0039AuthorRegisteredFixed>(e => e.Name == "Jane Austen");

    // Returning the Task works too — the test runner awaits it.
    [Fact]
    Task should_have_appended_exactly_one_event() =>
        _eventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```
