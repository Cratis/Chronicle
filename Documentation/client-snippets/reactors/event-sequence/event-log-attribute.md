```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record EventSequenceLogOrderPlaced(Guid OrderId);

[EventLog]
public class EventSequenceLocalAuditReactor : IReactor
{
    public Task OrderPlaced(EventSequenceLogOrderPlaced @event, EventContext context) =>
        WriteAuditAsync(@event.OrderId, context.Occurred);

    Task WriteAuditAsync(Guid orderId, DateTimeOffset occurred) => Task.CompletedTask;
}
```
