```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

[EventType("email-testing-order-shipped")]
public record TestingReactorScenarioEmailOrderShipped(string OrderId, string Carrier);

public interface ITestingReactorScenarioEmailService
{
    Task SendShippingConfirmation(string orderId, string carrier);
}

public class TestingReactorScenarioOrderNotificationReactor(ITestingReactorScenarioEmailService emailService) : IReactor
{
    public async Task OnOrderShipped(TestingReactorScenarioEmailOrderShipped @event, EventContext context)
    {
        await emailService.SendShippingConfirmation(@event.OrderId, @event.Carrier);
    }
}

public class when_order_is_shipped : Specification
{
    readonly ITestingReactorScenarioEmailService _emailService = Substitute.For<ITestingReactorScenarioEmailService>();
    ReactorScenario<TestingReactorScenarioOrderNotificationReactor> _scenario = default!;

    void Establish()
    {
        var services = new ServiceCollection()
            .AddSingleton(_emailService)
            .BuildServiceProvider();

        _scenario = new ReactorScenario<TestingReactorScenarioOrderNotificationReactor>(services);
    }

    Task Because() =>
        _scenario.Given
            .ForEventSource("order-123")
            .Events(new TestingReactorScenarioEmailOrderShipped("order-123", "DHL"));

    [Fact] async Task should_send_shipping_confirmation() =>
        await _emailService.Received(1).SendShippingConfirmation("order-123", "DHL");
}
```
