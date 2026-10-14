```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0055PaymentReceived(decimal Amount);

[EventSource("Order")]
[EventStream("Payments")]
public class Chr0055OrderEventSource : IEventSource;

// Error CHR0055: Chr0055OrderEventSource does not declare an event stream named "Payment".
[FromEventSource<Chr0055OrderEventSource>("Payment")]
public class Chr0055PaymentReactor : IReactor
{
    public Task Received(Chr0055PaymentReceived @event, EventContext context) => Task.CompletedTask;
}
```
