```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType("order-placed")]
[EventStore("orders")]
public record Chr0008ViolationOrderPlaced(decimal Amount);

[EventType("shipment-scheduled")]
[EventStore("shipping")]
public record Chr0008ViolationShipmentScheduled(string Destination);

// Error CHR0008: Reactor 'Chr0008ViolationOrderProcessor' handles event types from
// multiple event stores: "orders", "shipping". All event types in a reactor must
// originate from the same event store.
public class Chr0008ViolationOrderProcessor : IReactor
{
    public void Handle(Chr0008ViolationOrderPlaced @event) { }
    public void Handle(Chr0008ViolationShipmentScheduled @event) { }
}
```
