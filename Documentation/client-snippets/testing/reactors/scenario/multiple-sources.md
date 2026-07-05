```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

[EventType]
public record TestingReactorScenarioTenantActivated(string TenantId);

public interface ITestingReactorScenarioSyncService
{
    Task SyncTenant(string tenantId);
}

public class TestingReactorScenarioTenantSyncReactor(ITestingReactorScenarioSyncService syncService) : IReactor
{
    public Task TenantActivated(TestingReactorScenarioTenantActivated @event, EventContext context) =>
        syncService.SyncTenant(@event.TenantId);
}

public static class TestingReactorScenarioMultipleSources
{
    public static async Task Run()
    {
        var syncService = Substitute.For<ITestingReactorScenarioSyncService>();
        var services = new ServiceCollection()
            .AddSingleton(syncService)
            .BuildServiceProvider();

        var scenario = new ReactorScenario<TestingReactorScenarioTenantSyncReactor>(services);

        // Events from two different tenants
        await scenario.Given
            .ForEventSource("tenant-A")
            .Events(new TestingReactorScenarioTenantActivated("tenant-A"));

        await scenario.Given
            .ForEventSource("tenant-B")
            .Events(new TestingReactorScenarioTenantActivated("tenant-B"));

        // Both activations should have been handled
        await syncService.Received(2).SyncTenant(Arg.Any<string>());
    }
}
```
