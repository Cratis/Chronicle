```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using NSubstitute;

[EventType]
public record TestingReactorScenarioSomeEvent(string Value);

[EventType]
public record TestingReactorScenarioSomeOtherEvent(int Value);

public interface ITestingReactorScenarioService
{
    void DoSomething(string value);
}

public class TestingReactorScenarioMyReactor(ITestingReactorScenarioService service) : IReactor
{
    public Task SomeEvent(TestingReactorScenarioSomeEvent @event, EventContext context)
    {
        service.DoSomething(@event.Value);
        return Task.CompletedTask;
    }
}

public static class TestingReactorScenarioBasic
{
    public static async Task Run(IServiceProvider serviceProvider, ITestingReactorScenarioService myMock)
    {
        var someId = EventSourceId.New();
        var scenario = new ReactorScenario<TestingReactorScenarioMyReactor>(serviceProvider);
        await scenario.Given
            .ForEventSource(someId)
            .Events(new TestingReactorScenarioSomeEvent("value"), new TestingReactorScenarioSomeOtherEvent(42));

        // Assert on side-effects captured by mocks in serviceProvider
        myMock.Received(1).DoSomething("value");
    }
}
```
