```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class SubscriptionsImplicitFulfillmentReactor : IReactor
{
    public Task ShipmentDispatched(SubscriptionsImplicitShipmentDispatched @event, EventContext context)
        => HandleFulfillmentAsync(@event);

    Task HandleFulfillmentAsync(SubscriptionsImplicitShipmentDispatched @event) => Task.CompletedTask;
}

public class SubscriptionsImplicitOrderingReactor : IReactor
{
    public Task OrderPlaced(SubscriptionsImplicitOrderPlaced @event, EventContext context)
        => HandleOrderAsync(@event);

    Task HandleOrderAsync(SubscriptionsImplicitOrderPlaced @event) => Task.CompletedTask;
}
```
