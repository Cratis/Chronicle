```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
public record SubscriptionsExplicitShipmentDispatched(string OrderId);

public static class SubscriptionsExplicitBasic
{
    public static Task Run(IEventStore eventStore) =>
        eventStore.Subscriptions.Subscribe(
            "orders-from-fulfillment",
            "fulfillment-service",
            builder => builder.WithEventType<SubscriptionsExplicitShipmentDispatched>());
}
```
