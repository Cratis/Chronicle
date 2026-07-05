```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

[EventType]
public record TestingReadModelScenarioOrderCreated(string OrderId);

public record TestingReadModelScenarioOrderSummary(string OrderId, decimal Total);

public interface ITestingReadModelScenarioPricingService
{
    decimal GetBasePrice();
}

public class TestingReadModelScenarioOrderSummaryReducer(ITestingReadModelScenarioPricingService pricingService) : IReducerFor<TestingReadModelScenarioOrderSummary>
{
    public TestingReadModelScenarioOrderSummary OrderCreated(TestingReadModelScenarioOrderCreated @event, TestingReadModelScenarioOrderSummary? current, EventContext context) =>
        new(@event.OrderId, pricingService.GetBasePrice());
}

public static class TestingReadModelScenarioInjectingDependencies
{
    public static async Task Run()
    {
        var pricingService = Substitute.For<ITestingReadModelScenarioPricingService>();
        var services = new ServiceCollection()
            .AddSingleton(pricingService)
            .BuildServiceProvider();

        var scenario = new ReadModelScenario<TestingReadModelScenarioOrderSummary>(initialState: null, serviceProvider: services);
        await scenario.Given
            .ForEventSource("order-1")
            .Events(new TestingReadModelScenarioOrderCreated("order-1"));

        scenario.Instance!.Total.ShouldEqual(0m);
    }
}
```
