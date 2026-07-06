```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

public class ExplicitMetadataReactor : IReactor
{
    public EventForEventSourceId BookReserved(BookReserved @event, EventContext context) =>
        new(@event.MemberId, new MemberActivityRecorded(@event.Isbn))
        {
            EventStreamType = new EventStreamType("members"),
            Subject = new Subject(@event.MemberId.Value),
        };
}
```
