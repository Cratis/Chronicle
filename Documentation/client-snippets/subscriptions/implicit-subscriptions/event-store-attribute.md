```csharp
using Cratis.Chronicle.Events;

[EventType]
[EventStore("fulfillment-service")]
public record SubscriptionsImplicitShipmentDispatched(Guid OrderId, string TrackingNumber);

[EventType]
[EventStore("fulfillment-service")]
public record SubscriptionsImplicitShipmentFailed(Guid OrderId, string Reason);
```
