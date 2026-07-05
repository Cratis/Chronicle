```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

[EventType]
public record TestingReactorScenarioOrderShipped(string OrderId, string Carrier);

public interface ITestingReactorScenarioOrderRepository
{
    Task MarkAsShipped(string orderId);
}

public class TestingReactorScenarioOrderReactor(ITestingReactorScenarioOrderRepository orderRepository) : IReactor
{
    public Task OrderShipped(TestingReactorScenarioOrderShipped @event, EventContext context) =>
        orderRepository.MarkAsShipped(@event.OrderId);
}

public static class TestingReactorScenarioInjectingDependencies
{
    public static async Task Run()
    {
        var orderRepository = Substitute.For<ITestingReactorScenarioOrderRepository>();
        var services = new ServiceCollection()
            .AddSingleton(orderRepository)
            .BuildServiceProvider();

        var scenario = new ReactorScenario<TestingReactorScenarioOrderReactor>(services);
        await scenario.Given
            .ForEventSource("order-123")
            .Events(new TestingReactorScenarioOrderShipped("order-123", "FedEx"));

        await orderRepository.Received(1).MarkAsShipped("order-123");
    }
}
```
