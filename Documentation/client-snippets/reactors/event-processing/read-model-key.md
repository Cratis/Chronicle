```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;

[EventType]
public record ReactorOrderLineOrderPlaced(string CustomerId);

[EventType]
public record ReactorOrderLineAdded(string OrderId);

// The order read model is keyed by the order's event source id.
[FromEvent<ReactorOrderLineOrderPlaced>]
public record ReactorOrder([Key] string Id, string CustomerId);

public class OrderLineProcessingReactor : IReactor, ICanResolveReadModelKey
{
    public ReadModelKey Resolve(object @event, EventContext context) =>
        ((ReactorOrderLineAdded)@event).OrderId;

    // The line's own event source is not the order, so the key comes from the event.
    public Task OrderLineAdded(ReactorOrderLineAdded @event, ReactorOrder order) =>
        Task.CompletedTask;
}
```
