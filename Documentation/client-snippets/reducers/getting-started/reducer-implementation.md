```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record ReducersGettingStartedOrderCreated(Guid OrderId);

[EventType]
public record ReducersGettingStartedItemAddedToOrder(decimal Price, int Quantity);

[EventType]
public record ReducersGettingStartedItemRemovedFromOrder(decimal Price, int Quantity);

public class ReducersGettingStartedOrderSummaryReducer : IReducerFor<ReducersGettingStartedOrderSummary>
{
    public ReducersGettingStartedOrderSummary Created(ReducersGettingStartedOrderCreated @event, ReducersGettingStartedOrderSummary? current, EventContext context) =>
        new(
            OrderId: @event.OrderId,
            TotalAmount: 0m,
            ItemCount: 0,
            LastUpdated: context.Occurred);

    public ReducersGettingStartedOrderSummary? ItemAdded(ReducersGettingStartedItemAddedToOrder @event, ReducersGettingStartedOrderSummary? current, EventContext context)
    {
        if (current is null) return null; // Skip if order not created yet

        return current with
        {
            TotalAmount = current.TotalAmount + (@event.Price * @event.Quantity),
            ItemCount = current.ItemCount + @event.Quantity,
            LastUpdated = context.Occurred
        };
    }

    public ReducersGettingStartedOrderSummary? ItemRemoved(ReducersGettingStartedItemRemovedFromOrder @event, ReducersGettingStartedOrderSummary? current, EventContext context)
    {
        if (current is null) return null; // Skip if order not created yet

        return current with
        {
            TotalAmount = current.TotalAmount - (@event.Price * @event.Quantity),
            ItemCount = current.ItemCount - @event.Quantity,
            LastUpdated = context.Occurred
        };
    }
}
```
