```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record SubscriptionsExplicitOrderPlaced(Guid OrderId, decimal Amount);

public class SubscriptionsExplicitIncomingOrdersReactor : IReactor
{
    public Task OrderPlaced(SubscriptionsExplicitOrderPlaced @event, EventContext context)
        => HandleIncomingOrderAsync(@event.OrderId, @event.Amount);

    Task HandleIncomingOrderAsync(Guid id, decimal amount) => Task.CompletedTask;
}
```
