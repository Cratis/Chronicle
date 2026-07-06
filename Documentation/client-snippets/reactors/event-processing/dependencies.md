```csharp
using Cratis.Chronicle.Events;
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
public record EventProcessingOrderPlaced;

public record EventProcessingOrder(string Id, decimal Total);

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
