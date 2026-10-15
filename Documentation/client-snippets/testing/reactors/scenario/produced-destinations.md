```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;

[EventType]
public record TestingReactorScenarioDestinationPaymentReceived(decimal Amount);

[EventType]
public record TestingReactorScenarioDestinationPaymentRecorded(decimal Amount);

public class TestingReactorScenarioDestinationReactor : IReactor
{
    public EventForEventSourceId Handle(TestingReactorScenarioDestinationPaymentReceived @event) =>
        new("ledger", new TestingReactorScenarioDestinationPaymentRecorded(@event.Amount))
        {
            EventSourceType = "Ledger",
            EventStreamType = "Payments",
            EventStreamId = "monthly"
        };
}

public static class TestingReactorScenarioProducedDestinations
{
    public static async Task Run()
    {
        using var scenario = new ReactorScenario<TestingReactorScenarioDestinationReactor>();
        await scenario.Given.ForEventSource(EventSourceId.New())
            .Events(new TestingReactorScenarioDestinationPaymentReceived(100));

        scenario.ShouldHaveProduced<TestingReactorScenarioDestinationPaymentRecorded>(new EventSourceId("ledger"));
        scenario.ShouldHaveProduced<TestingReactorScenarioDestinationPaymentRecorded>((produced, @event) =>
            produced.EventSourceId == new EventSourceId("ledger") &&
            produced.EventSourceType == new EventSourceType("Ledger") &&
            produced.EventStreamType == new EventStreamType("Payments") &&
            produced.EventStreamId == new EventStreamId("monthly") && @event.Amount == 100);

        foreach (var produced in scenario.ProducedEvents)
        {
            Console.WriteLine($"{produced.Event.GetType().Name}: {produced.EventSourceId.Value}/{produced.EventStreamType.Value}/{produced.EventStreamId.Value}");
        }
    }
}
```
