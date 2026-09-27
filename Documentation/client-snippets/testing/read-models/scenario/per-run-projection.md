```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using NSubstitute;

[EventType]
public record TestingPerRunProjectionEvent(string Name);

public record TestingPerRunProjectionModel(string? Name);

public static class TestingPerRunProjectionExample
{
    public static async Task Run()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(TestingPerRunProjectionEvent)]);
        var scenario = new ReadModelScenario<TestingPerRunProjectionModel>(null, new Defaults(artifacts))
            .WithProjection(builder => builder.From<TestingPerRunProjectionEvent>(from =>
                from.Set(model => model.Name).To(@event => @event.Name)));
        await scenario.Given.ForEventSource(EventSourceId.New()).Events(new TestingPerRunProjectionEvent("example"));
        var result = scenario.Instance;
    }
}
```
