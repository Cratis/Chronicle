```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;

[EventType]
public record TestingReactorScenarioRoutedPaymentReceived(decimal Amount);

[EventType]
public record TestingReactorScenarioPaymentRouteObserved(decimal Amount, string SourceType, string StreamType, string StreamId);

public class TestingReactorScenarioRouteReactor : IReactor
{
    public Task<TestingReactorScenarioPaymentRouteObserved> Handle(
        TestingReactorScenarioRoutedPaymentReceived @event, EventContext context) =>
        Task.FromResult(new TestingReactorScenarioPaymentRouteObserved(
            @event.Amount, context.EventSourceType.Value, context.EventStreamType.Value, context.EventStreamId.Value));
}

public static class TestingReactorScenarioRoutedEvents
{
    public static async Task Run()
    {
        using var scenario = new ReactorScenario<TestingReactorScenarioRouteReactor>();
        var orderId = EventSourceId.New();

        await scenario.Given.ForEventSource(orderId)
            .OnStream(eventSourceType: "Order", eventStreamType: "Payments", eventStreamId: "monthly")
            .Events(new TestingReactorScenarioRoutedPaymentReceived(100));

        scenario.ShouldHaveProduced<TestingReactorScenarioPaymentRouteObserved>(observed =>
            observed.Amount == 100 && observed.SourceType == "Order" &&
            observed.StreamType == "Payments" && observed.StreamId == "monthly");
    }
}
```
