```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record FanOutStockDecreased(Isbn Isbn, int Quantity);

public class ReservationFanOutReactor : IReactor
{
    public IEnumerable<EventForEventSourceId> BookReserved(BookReserved @event, EventContext context) =>
    [
        new(@event.MemberId, new MemberActivityRecorded(@event.Isbn)),
        new(@event.Isbn, new FanOutStockDecreased(@event.Isbn, 1)),
    ];
}
```
