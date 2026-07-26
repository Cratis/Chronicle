```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

[EventType]
public record TestingReactorScenarioVibeCancelled();

[EventType]
public record TestingReactorScenarioVibeStarted(string Host);

[Passive]
[FromEvent<TestingReactorScenarioVibeStarted>]
public record TestingReactorScenarioVibeAttendees([Key] string Id, string Host);

public interface ITestingReactorScenarioNotifier
{
    Task Notify(string host);
}

public class TestingReactorScenarioCancellationReactor(ITestingReactorScenarioNotifier notifier) : IReactor
{
    // The read model is a handler-method parameter — Chronicle materializes it for the vibe.
    public Task VibeCancelled(
        TestingReactorScenarioVibeCancelled @event,
        EventContext context,
        TestingReactorScenarioVibeAttendees attendees) =>
        notifier.Notify(attendees.Host);
}

public static class TestingReactorScenarioReadModel
{
    public static async Task Run()
    {
        var notifier = Substitute.For<ITestingReactorScenarioNotifier>();
        var vibeId = EventSourceId.New();

        var scenario = new ReactorScenario<TestingReactorScenarioCancellationReactor>();
        scenario.Services.AddSingleton(notifier);

        // Seed the read-model parameter, then drive the triggering event.
        scenario.Given.ForEventSourceId(vibeId).ReadModel(new TestingReactorScenarioVibeAttendees(vibeId, "Ada"));
        await scenario.Given
            .ForEventSource(vibeId)
            .Events(new TestingReactorScenarioVibeCancelled());

        await notifier.Received(1).Notify("Ada");
    }
}
```
