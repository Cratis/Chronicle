```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

[EventType]
public record TestingReactorScenarioServicesBookingCancelled(string BookingId);

public interface ITestingReactorScenarioServicesNotifications
{
    Task Notify(string message);
}

public class TestingReactorScenarioServicesCancellationReactor(ITestingReactorScenarioServicesNotifications notifications) : IReactor
{
    public Task BookingCancelled(TestingReactorScenarioServicesBookingCancelled @event, EventContext context) =>
        notifications.Notify($"Booking {@event.BookingId} was cancelled.");
}

public static class TestingReactorScenarioServices
{
    public static async Task Run()
    {
        var notifications = Substitute.For<ITestingReactorScenarioServicesNotifications>();

        var scenario = new ReactorScenario<TestingReactorScenarioServicesCancellationReactor>();
        scenario.Services.AddSingleton(notifications);

        await scenario.Given
            .ForEventSource("booking-123")
            .Events(new TestingReactorScenarioServicesBookingCancelled("booking-123"));

        await notifications.Received(1).Notify("Booking booking-123 was cancelled.");
    }
}
```
