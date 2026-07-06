```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;

[EventType]
public record ReactorOrderLineAdded(string OrderId);

public record ReactorOrderLine(string Id, string ProductId, int Quantity);

public class OrderLineProcessingReactor : IReactor, ICanResolveReadModelKey
{
    public ReadModelKey Resolve(object @event, EventContext context) =>
        ((ReactorOrderLineAdded)@event).OrderId;

    public Task OrderLineAdded(ReactorOrderLineAdded @event, ReactorOrderLine order) =>
        Task.CompletedTask;
}
```
