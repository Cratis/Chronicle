```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record ActivityLogged(Isbn Isbn);

public class MixedSideEffectsReactor : IReactor
{
    public IEnumerable<object> BookReserved(BookReserved @event, EventContext context) =>
    [
        new ActivityLogged(@event.Isbn),
        new EventForEventSourceId(@event.MemberId, new MemberActivityRecorded(@event.Isbn)),
    ];
}
```
