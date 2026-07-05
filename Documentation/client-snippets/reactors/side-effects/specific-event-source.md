```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record MemberActivityRecorded(Isbn Isbn);

public class ReservationReactor : IReactor
{
    public EventForEventSourceId BookReserved(BookReserved @event, EventContext context) =>
        new(@event.MemberId, new MemberActivityRecorded(@event.Isbn));
}
```
