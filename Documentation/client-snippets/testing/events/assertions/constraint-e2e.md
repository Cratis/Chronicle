```csharp
using Cratis.Concepts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Specifications;
using Xunit;

// Email is a ConceptAs<string> — uniqueness on a domain value works in-process,
// so the constraint no longer needs to be hidden behind #if !DEBUG.
public record TestingAssertionsEmail(string Value) : ConceptAs<string>(Value);

[EventType]
public record TestingAssertionsAuthorRegisteredWithEmail([property: Unique("TestingAssertionsUniqueAuthorEmail")] TestingAssertionsEmail Email);

public class when_registering_an_author_with_a_taken_email : Specification, IDisposable
{
    readonly EventScenario _scenario = new();
    AppendResult _result = default!;

    Task Establish() =>
        _scenario.Given
            .ForEventSource(EventSourceId.New())
            .Events(new TestingAssertionsAuthorRegisteredWithEmail(new("john@doe.com")));

    async Task Because() =>
        _result = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new TestingAssertionsAuthorRegisteredWithEmail(new("john@doe.com")));

    [Fact] void should_be_rejected() =>
        _result.ShouldBeFailed();

    [Fact] void should_report_the_constraint() =>
        _result.ShouldHaveConstraintViolation("TestingAssertionsUniqueAuthorEmail");

    public void Dispose() => _scenario.Dispose();
}
```
