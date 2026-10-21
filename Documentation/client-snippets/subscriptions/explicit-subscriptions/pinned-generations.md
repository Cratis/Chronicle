```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

[EventType("subscription-stock-adjusted", generation: 2)]
[Public]
public record SubscriptionStockAdjusted(int Quantity);

public static class PinnedInventorySubscription
{
    public static Task Register(IEventStore eventStore) =>
        eventStore.Subscriptions.Subscribe(
            "inventory-updates",
            "warehouse-service",
            builder => builder
                .WithGenerationDelivery(EventGenerationDelivery.Pinned)
                .WithEventType<SubscriptionStockAdjusted>());

    public static Task RegisterByIdentifier(IEventStore eventStore) =>
        eventStore.Subscriptions.Subscribe(
            "inventory-updates",
            "warehouse-service",
            builder => builder
                .WithGenerationDelivery(EventGenerationDelivery.Pinned)
                .WithEventType("subscription-stock-adjusted", 2));
}
```
