```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0022OrderPlaced(Guid OrderId, decimal Total);

[EventType]
public record Chr0022InvoiceRaised(Guid OrderId, decimal Total);

public class Chr0022Invoicing : IReactor
{
    // Warning CHR0022: this method returns a side-effect event, so Chronicle appends Chr0022InvoiceRaised
    // whenever Chr0022OrderPlaced is observed — including during replay, which would append it again.
    public Chr0022InvoiceRaised OrderPlaced(Chr0022OrderPlaced @event) =>
        new(@event.OrderId, @event.Total);
}
```
