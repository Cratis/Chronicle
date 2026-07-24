```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;

[EventType]
public record TestingReactorScenarioVibeStarted(string Host);

public record TestingReactorScenarioSendReminder(string Host);

public class TestingReactorScenarioReminderReactor : IReactor
{
    public Task<TestingReactorScenarioSendReminder> VibeStarted(TestingReactorScenarioVibeStarted @event) =>
        Task.FromResult(new TestingReactorScenarioSendReminder(@event.Host));
}

public static class TestingReactorScenarioProducedSideEffects
{
    public static async Task Run()
    {
        var vibeId = EventSourceId.New();
        var scenario = new ReactorScenario<TestingReactorScenarioReminderReactor>();

        await scenario.Given
            .ForEventSource(vibeId)
            .Events(new TestingReactorScenarioVibeStarted("Ada"));

        scenario.ShouldHaveProduced<TestingReactorScenarioSendReminder>(reminder => reminder.Host == "Ada");
    }
}
```
