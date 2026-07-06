```csharp
// FulfillmentService.Events/ShipmentDispatched.cs
using Cratis.Chronicle.Events;

[EventType]
[EventStore("fulfillment-service")]
public record SubscriptionsImplicitPkgShipmentDispatched(Guid OrderId, string TrackingNumber);

[EventType]
[EventStore("fulfillment-service")]
public record SubscriptionsImplicitPkgShipmentFailed(Guid OrderId, string Reason);
```
