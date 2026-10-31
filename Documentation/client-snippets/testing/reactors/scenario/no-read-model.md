```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

[EventType]
public record TestingReactorNoReadModelVibeStarted(string Host);

[EventType]
public record TestingReactorNoReadModelVibeCancelled();

[FromEvent<TestingReactorNoReadModelVibeStarted>]
public record TestingReactorNoReadModelAttendees([Key] string Id, string Host);

public interface ITestingReactorNoReadModelNotifier
{
    Task Notify(string host);
}

public class TestingReactorNoReadModelCancellationReactor(ITestingReactorNoReadModelNotifier notifier) : IReactor
{
    public Task Handle(
        TestingReactorNoReadModelVibeCancelled @event,
        TestingReactorNoReadModelAttendees? attendees) =>
        attendees is null ? Task.CompletedTask : notifier.Notify(attendees.Host);
}

public static class TestingReactorNoReadModelExample
{
    public static async Task Run()
    {
        var notifier = Substitute.For<ITestingReactorNoReadModelNotifier>();
        var vibeId = EventSourceId.New();
        using var scenario = new ReactorScenario<TestingReactorNoReadModelCancellationReactor>();
        scenario.Services.AddSingleton(notifier);

        // Declare intentional absence so the handler receives null instead of a seeding error.
        await scenario.Given.ForEventSourceId(vibeId)
            .NoReadModel<TestingReactorNoReadModelAttendees>()
            .Events(new TestingReactorNoReadModelVibeCancelled());

        await notifier.DidNotReceive().Notify(Arg.Any<string>());
    }
}
```
