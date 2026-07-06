```csharp
using Cratis.Chronicle;

public static class SubscriptionsExplicitNoFilter
{
    public static Task Run(IEventStore eventStore) =>
        // All events from fulfillment-service outbox will be forwarded
        eventStore.Subscriptions.Subscribe(
            "all-fulfillment-events",
            "fulfillment-service");
}
```
