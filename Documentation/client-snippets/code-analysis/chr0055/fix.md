```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0055FixPaymentReceived(decimal Amount);

[EventSource("Order")]
[EventStream("Payments")]
public class Chr0055FixOrderEventSource : IEventSource;

// The stream name matches the declared stream exactly.
[FromEventSource<Chr0055FixOrderEventSource>("Payments")]
public class Chr0055FixPaymentReactor : IReactor
{
    public Task Received(Chr0055FixPaymentReceived @event, EventContext context) => Task.CompletedTask;
}
```
