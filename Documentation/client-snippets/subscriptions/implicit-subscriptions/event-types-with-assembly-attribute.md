```csharp
// FulfillmentService.Events.Contracts/ShipmentDispatched.cs
using Cratis.Chronicle.Events;

namespace SubscriptionsImplicitAssemblyExample
{
    [EventType]
    public record ShipmentDispatched(Guid OrderId, string TrackingNumber);

    [EventType]
    public record ShipmentFailed(Guid OrderId, string Reason);
}
```
