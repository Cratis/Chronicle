```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;

[EventType]
public record ReservationConfirmed(Isbn Isbn);

public class ReservationConfirmationReactor : IReactor
{
    public EventsWithConcurrencyScopes BookReserved(BookReserved @event, EventContext context) =>
        new(
            [new EventForEventSourceId(context.EventSourceId, new ReservationConfirmed(@event.Isbn))],
            new Dictionary<EventSourceId, ConcurrencyScope>
            {
                [context.EventSourceId] = new(
                    context.SequenceNumber,
                    EventSourceId: context.EventSourceId),
            });
}
```
