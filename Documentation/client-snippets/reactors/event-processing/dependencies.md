```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;

public interface IReactorShippingService
{
    Task Schedule(EventProcessingOrder order, decimal price);
}

public interface IReactorPricingService
{
    decimal PriceFor(EventProcessingOrder order);
}

[EventType]
public record EventProcessingOrderPlaced(decimal Total);

// A read model is resolved by Chronicle only when a projection or reducer builds it.
[FromEvent<EventProcessingOrderPlaced>]
public record EventProcessingOrder([Key] string Id, decimal Total);

public class EventProcessingOrderProcessor(IReactorShippingService shipping) : IReactor
{
    public async Task OrderPlaced(
        EventProcessingOrderPlaced @event,
        EventContext context,
        EventProcessingOrder order,
        IReactorPricingService pricing)
    {
        await shipping.Schedule(order, pricing.PriceFor(order));
    }
}
```
