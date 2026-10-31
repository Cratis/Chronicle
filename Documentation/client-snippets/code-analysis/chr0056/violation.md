```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0056PaymentReceived(decimal Amount);

[EventSource("orders")]
[EventStream("placement")]
public class Chr0056OrderEventSource : IEventSource;

// Error CHR0056 twice: both attributes contradict the typed filter, which already means
// event source type "orders" and event stream type "placement".
[FromEventSource<Chr0056OrderEventSource>("placement")]
[EventSourceType("invoices")]
[EventStreamType("payment")]
public class Chr0056PaymentReactor : IReactor
{
    public Task Received(Chr0056PaymentReceived @event, EventContext context) => Task.CompletedTask;
}
```
