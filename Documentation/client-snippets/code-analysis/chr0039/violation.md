```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Xunit;

[EventType]
public record Chr0039AuthorRegistered(string Name);

public class Chr0039WhenRegisteringAnAuthor
{
    readonly IEventLog _eventLog = default!;

    // Warning CHR0039: 'ShouldHaveAppendedEvent' returns a Task that is never awaited, so the
    // assertion can never fail. The fact is 'void', so the compiler's own CS4014 stays silent —
    // this spec passes even though no Chr0039AuthorRegistered carries that name.
    [Fact]
    void should_have_appended_the_event() =>
        _eventLog.ShouldHaveAppendedEvent<Chr0039AuthorRegistered>(e => e.Name == "Jane Austen");

    // Warning CHR0039: the same trap in a block body.
    [Fact]
    void should_have_appended_exactly_one_event()
    {
        _eventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    }
}
```
