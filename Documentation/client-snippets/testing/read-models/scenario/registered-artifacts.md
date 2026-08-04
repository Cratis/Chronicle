```csharp
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

public static class TestingReadModelScenarioRegisteredArtifacts
{
    public static void Run()
    {
        var scenario = new ReadModelScenario<TestingReadModelScenarioModelBoundDeliveryStatus>();

        // The registry Chronicle discovered its artifacts from - no reflection of your own needed
        var artifacts = scenario.ClientArtifactsProvider;
        artifacts.ModelBoundProjections.ShouldContain(typeof(TestingReadModelScenarioModelBoundDeliveryStatus));
        artifacts.EventTypes.ShouldContain(typeof(TestingReadModelScenarioModelBoundShipmentDispatched));

        // The same registry, reachable without a scenario
        Defaults.Instance.ClientArtifactsProvider.Reactors.ShouldNotBeNull();
    }
}
```
