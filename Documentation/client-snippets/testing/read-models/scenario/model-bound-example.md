```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Specifications;

[EventType]
public record TestingReadModelScenarioModelBoundShipmentDispatched(string Carrier);

[EventType]
public record TestingReadModelScenarioModelBoundShipmentDelivered(DateTimeOffset DeliveredAt);

[FromEvent<TestingReadModelScenarioModelBoundShipmentDispatched>]
[FromEvent<TestingReadModelScenarioModelBoundShipmentDelivered>]
public record TestingReadModelScenarioModelBoundDeliveryStatus(
    [Key] string ShipmentId,
    string Carrier,
    DateTimeOffset? DeliveredAt);

public static class TestingReadModelScenarioModelBoundExample
{
    public static async Task Run()
    {
        var shipmentId = "shipment-1";
        var scenario = new ReadModelScenario<TestingReadModelScenarioModelBoundDeliveryStatus>();
        await scenario.Given
            .ForEventSource(shipmentId)
            .Events(
                new TestingReadModelScenarioModelBoundShipmentDispatched("FedEx"),
                new TestingReadModelScenarioModelBoundShipmentDelivered(DateTimeOffset.UtcNow));

        scenario.Instance!.Carrier.ShouldEqual("FedEx");
        scenario.Instance!.DeliveredAt.HasValue.ShouldBeTrue();
    }
}
```
