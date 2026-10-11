```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;
using Xunit;

[EventType]
public record TestingReadModelScenarioPaymentReceived(decimal Amount);

[FromEvent<TestingReadModelScenarioPaymentReceived>]
public record TestingReadModelScenarioPaymentRoute(
    [Key] Guid Id,
    decimal Amount,
    [SetFromContext<TestingReadModelScenarioPaymentReceived>(nameof(EventContext.EventSourceType))] EventSourceType SourceType,
    [SetFromContext<TestingReadModelScenarioPaymentReceived>(nameof(EventContext.EventStreamType))] EventStreamType StreamType,
    [SetFromContext<TestingReadModelScenarioPaymentReceived>(nameof(EventContext.EventStreamId))] EventStreamId StreamId);

public class when_seeding_a_routed_payment : Specification
{
    readonly EventSourceId _orderId = EventSourceId.New();
    readonly ReadModelScenario<TestingReadModelScenarioPaymentRoute> _scenario = new();

    Task Because() =>
        _scenario.Given
            .ForEventSource(_orderId)
            .OnStream(eventSourceType: "Order", eventStreamType: "Payments", eventStreamId: "2026-10")
            .Events(new TestingReadModelScenarioPaymentReceived(42m));

    [Fact] void should_see_the_source_type() => _scenario.Instance!.SourceType.ShouldEqual(new EventSourceType("Order"));
    [Fact] void should_see_the_stream_type() => _scenario.Instance!.StreamType.ShouldEqual(new EventStreamType("Payments"));
    [Fact] void should_see_the_stream_id() => _scenario.Instance!.StreamId.Value.ShouldEqual("2026-10");
}
```
