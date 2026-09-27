```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;

[EventType]
public record ScenarioWithProjectionNameChanged(string Name);

public record ScenarioWithProjectionReadModel(string Name);

public class ScenarioWithProjection
{
    public async Task Run(IClientArtifactsProvider artifacts, EventSourceId id)
    {
        var scenario = new ReadModelScenario<ScenarioWithProjectionReadModel>(null, new Defaults(artifacts))
            .WithProjection(builder => builder.From<ScenarioWithProjectionNameChanged>(from =>
                from.Set(model => model.Name).To(e => e.Name)));
        await scenario.Given.ForEventSource(id).Events(new ScenarioWithProjectionNameChanged("example"));
        var result = scenario.Instance;
    }
}
```
