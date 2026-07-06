```csharp
using Cratis.Chronicle;

public static class SubscriptionsExplicitUnsubscribe
{
    public static Task Run(IEventStore eventStore) =>
        eventStore.Subscriptions.Unsubscribe("orders-from-fulfillment");
}
```
