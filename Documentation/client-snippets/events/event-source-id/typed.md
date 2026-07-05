```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

// A strongly-typed domain concept
public record EventSourceIdCustomerId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static EventSourceIdCustomerId New() => new(Guid.NewGuid());
}

[EventType]
public record EventSourceIdOrderPlaced(EventSourceIdCustomerId CustomerId, decimal Total);

public class EventSourceIdOrderService(IEventLog eventLog)
{
    public Task PlaceOrder(decimal total)
    {
        var customerId = EventSourceIdCustomerId.New();

        // The typed identifier converts implicitly to EventSourceId — no manual conversion needed
        return eventLog.Append(customerId, new EventSourceIdOrderPlaced(customerId, total));
    }
}
```
