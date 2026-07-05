```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;
using Xunit;

[EventType]
public record TestingReadModelScenarioSomeEvent(string Value);

[EventType]
public record TestingReadModelScenarioSomeOtherEvent(int Value);

[FromEvent<TestingReadModelScenarioSomeEvent>]
public record TestingReadModelScenarioMyReadModel([Key] Guid Id, string Value);

public class when_projecting_events : Specification
{
    readonly EventSourceId _eventSourceId = EventSourceId.New();
    readonly ReadModelScenario<TestingReadModelScenarioMyReadModel> _scenario = new();

    Task Because() =>
        _scenario.Given
            .ForEventSource(_eventSourceId)
            .Events(new TestingReadModelScenarioSomeEvent("expected value"), new TestingReadModelScenarioSomeOtherEvent(42));

    [Fact] void should_project_the_value() =>
        _scenario.Instance!.Value.ShouldEqual("expected value");
}
```
