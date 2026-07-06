```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
public record SubscriptionsExplicitStockAdjusted(string ItemId, int Delta);

public static class SubscriptionsExplicitNamingConvention
{
    public static async Task Run(IEventStore eventStore)
    {
        // subscription-id format: {target}-from-{source}
        await eventStore.Subscriptions.Subscribe(
            "orders-from-fulfillment",
            "fulfillment-service",
            builder => builder.WithEventType<SubscriptionsExplicitShipmentDispatched>());

        await eventStore.Subscriptions.Subscribe(
            "inventory-from-warehouse",
            "warehouse-service",
            builder => builder.WithEventType<SubscriptionsExplicitStockAdjusted>());
    }
}
```
