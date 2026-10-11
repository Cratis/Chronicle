```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Reactors;

[EventSource("TestingReactorScenarioFilteredOrder")]
[EventStream("Payments")]
public record TestingReactorScenarioFilteredOrderSource : IEventSource;

[EventType]
public record TestingReactorScenarioFilteredPaymentReceived(decimal Amount);

[EventType]
public record TestingReactorScenarioFilteredPaymentAccepted(decimal Amount);

[FromEventSource<TestingReactorScenarioFilteredOrderSource>("Payments")]
public class TestingReactorScenarioFilteredPaymentReactor : IReactor
{
    public Task<TestingReactorScenarioFilteredPaymentAccepted> Handle(TestingReactorScenarioFilteredPaymentReceived @event) =>
        Task.FromResult(new TestingReactorScenarioFilteredPaymentAccepted(@event.Amount));
}

public static class TestingReactorScenarioObserverFilters
{
    public static async Task Run()
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(TestingReactorScenarioFilteredPaymentReceived), typeof(TestingReactorScenarioFilteredPaymentAccepted)],
            EventSources = [typeof(TestingReactorScenarioFilteredOrderSource)]
        };
        using var scenario = new ReactorScenario<TestingReactorScenarioFilteredPaymentReactor>(new Defaults(artifacts));
        var orderId = EventSourceId.New();

        await scenario.Given.ForEventSource(orderId)
            .OnStream(eventSourceType: "Customer", eventStreamType: "Payments")
            .Events(new TestingReactorScenarioFilteredPaymentReceived(100));
        await scenario.Given.ForEventSource(orderId)
            .OnStream(eventSourceType: "TestingReactorScenarioFilteredOrder", eventStreamType: "Shipping")
            .Events(new TestingReactorScenarioFilteredPaymentReceived(100));
        scenario.ShouldNotHaveProduced<TestingReactorScenarioFilteredPaymentAccepted>();

        await scenario.Given.ForEventSource(orderId)
            .OnStream(eventSourceType: "TestingReactorScenarioFilteredOrder", eventStreamType: "Payments")
            .Events(new TestingReactorScenarioFilteredPaymentReceived(100));
        scenario.ShouldHaveProduced<TestingReactorScenarioFilteredPaymentAccepted>(accepted => accepted.Amount == 100);
    }
}
```
