```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0022OrderPlacedFixed(Guid OrderId, decimal Total);

[EventType]
public record Chr0022InvoiceRaisedFixed(Guid OrderId, decimal Total);

public class Chr0022InvoicingFixed : IReactor
{
    // [OnceOnly] makes Chronicle run the handler a single time per event source and skip it during
    // replays, so the side-effect event is appended exactly once.
    [OnceOnly]
    public Chr0022InvoiceRaisedFixed OrderPlaced(Chr0022OrderPlacedFixed @event) =>
        new(@event.OrderId, @event.Total);
}
```
