```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record SubscriptionsOutboxInboxOrderPlaced(Guid OrderId);

public class SubscriptionsOutboxInboxIncomingOrdersReactor : IReactor
{
    public Task OrderPlaced(SubscriptionsOutboxInboxOrderPlaced @event, EventContext context)
    {
        // Handles OrderPlaced events from any subscribed source event store
        return ProcessAsync(@event.OrderId);
    }

    Task ProcessAsync(Guid orderId) => Task.CompletedTask;
}
```
