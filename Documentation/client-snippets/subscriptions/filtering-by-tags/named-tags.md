```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
[Public]
public record TagFilterOrderShipped(string OrderId);

public static class SubscriptionsTagFilterNamedTags
{
    public static Task Run(IEventStore eventStore) =>
        eventStore.Subscriptions.Subscribe(
            "orders-for-europe",
            "warehouse-service",
            builder => builder
                .WithEventType<TagFilterOrderShipped>()
                .WithNamedTag("region", "eu")
                .WithNamedTag("region", "uk")      // OR with the line above: eu or uk
                .WithNamedTag("priority", "high"));  // AND: ... and priority high
}
```
