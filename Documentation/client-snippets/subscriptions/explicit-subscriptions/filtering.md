```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
public record SubscriptionsExplicitStockReserved(string ItemId, int Quantity);

public static class SubscriptionsExplicitFiltering
{
    public static Task Run(IEventStore eventStore) =>
        eventStore.Subscriptions.Subscribe(
            "inventory-updates",
            "warehouse-service",
            builder => builder
                .WithEventType<SubscriptionsExplicitStockAdjusted>()
                .WithEventType<SubscriptionsExplicitStockReserved>());
}
```
