```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;

[EventType]
public record PerRunScenarioEvent(string Name);

public record PerRunScenarioModel(string? Name);

public class PerRunProjectionSpec(IClientArtifactsProvider artifacts)
{
    public async Task<PerRunScenarioModel?> Run(EventSourceId id)
    {
        var scenario = new ReadModelScenario<PerRunScenarioModel>(null, new Defaults(artifacts))
            .WithProjection(builder => builder.From<PerRunScenarioEvent>(from =>
                from.Set(model => model.Name).To(e => e.Name)));
        await scenario.Given.ForEventSource(id).Events(new PerRunScenarioEvent("example"));
        return scenario.Instance;
    }
}
```
