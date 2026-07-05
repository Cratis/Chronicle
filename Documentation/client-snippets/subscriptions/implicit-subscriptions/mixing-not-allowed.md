```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
[EventStore("ordering-service")]
public record SubscriptionsImplicitOrderPlaced(Guid OrderId, decimal Amount);

// ❌ This will throw MultipleEventStoresDefined
public class SubscriptionsImplicitInvalidReactor : IReactor
{
    // SubscriptionsImplicitShipmentDispatched has [EventStore("fulfillment-service")]
    public Task Handle(SubscriptionsImplicitShipmentDispatched @event) => Task.CompletedTask;

    // SubscriptionsImplicitOrderPlaced has [EventStore("ordering-service")]
    public Task Handle(SubscriptionsImplicitOrderPlaced @event) => Task.CompletedTask;
}
```
