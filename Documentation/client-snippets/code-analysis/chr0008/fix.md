```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType("order-placed")]
[EventStore("orders")]
public record Chr0008FixOrderPlaced(decimal Amount);

[EventType("order-shipped")]
[EventStore("orders")]
public record Chr0008FixOrderShipped(string Destination);

// All event types belong to the same event store: "orders".
public class Chr0008FixOrderProcessor : IReactor
{
    public void Handle(Chr0008FixOrderPlaced @event) { }
    public void Handle(Chr0008FixOrderShipped @event) { }
}
```
