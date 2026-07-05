```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

[EventType]
public record TestingReadModelScenarioItemAdded;

public record TestingReadModelScenarioCountingReadModel(int Count);

public class TestingReadModelScenarioCountingReducer : IReducerFor<TestingReadModelScenarioCountingReadModel>
{
    public TestingReadModelScenarioCountingReadModel ItemAdded(TestingReadModelScenarioItemAdded @event, TestingReadModelScenarioCountingReadModel? current, EventContext context) =>
        current is null ? new(1) : current with { Count = current.Count + 1 };
}

public static class TestingReadModelScenarioInitialState
{
    public static async Task Run()
    {
        var myId = EventSourceId.New();
        var initial = new TestingReadModelScenarioCountingReadModel(10);
        var scenario = new ReadModelScenario<TestingReadModelScenarioCountingReadModel>(initial);
        await scenario.Given
            .ForEventSource(myId)
            .Events(new TestingReadModelScenarioItemAdded());

        scenario.Instance!.Count.ShouldEqual(11);
    }
}
```
