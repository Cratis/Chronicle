```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

[EventType]
public record Chr0056FixPaymentReceived(decimal Amount);

[EventSource("orders")]
[EventStream("placement")]
public class Chr0056FixOrderEventSource : IEventSource;

// The typed filter alone says everything. Drop the string attributes.
[FromEventSource<Chr0056FixOrderEventSource>("placement")]
public class Chr0056FixPaymentReactor : IReactor
{
    public Task Received(Chr0056FixPaymentReceived @event, EventContext context) => Task.CompletedTask;
}
```
