```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Reactors;
using NSubstitute;

[EventType]
public record TestingPerRunReactorEvent(string Value);

public class TestingPerRunReactor : IReactor
{
    public Task OnEvent(TestingPerRunReactorEvent @event) => Task.CompletedTask;
}

public static class TestingPerRunReactorExample
{
    public static async Task Run()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(TestingPerRunReactorEvent)]);
        var scenario = new ReactorScenario<TestingPerRunReactor>(new Defaults(artifacts));
        await scenario.Given.ForEventSource(EventSourceId.New()).Events(new TestingPerRunReactorEvent("example"));
    }
}
```
