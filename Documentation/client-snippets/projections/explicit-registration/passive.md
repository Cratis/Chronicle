```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record ShipmentDispatched(string Carrier);

public record ShipmentState(string Carrier);

public class ShipmentStateRegistration
{
    public async Task<ShipmentState?> Current(IEventStore eventStore, string shipmentId)
    {
        await eventStore.Projections.Register<ShipmentState>(
            projection => projection.Passive().From<ShipmentDispatched>(),
            id: "shipment-state");

        // A passive projection is never materialized - each read computes the instance from its events.
        return await eventStore.ReadModels.GetInstanceById<ShipmentState>(shipmentId);
    }
}
```
