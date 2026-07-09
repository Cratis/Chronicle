```csharp
using Cratis.Chronicle.Events;

// Error CHR0027: Chr0027PlaceOrder both implements ICanProvideEventStreamId and declares a
// non-null [EventStreamId] — Chronicle throws AmbiguousEventStreamId at startup. Remove one;
// use [EventStreamId(null)] to defer to the interface.
[EventStreamId("orders")]
public class Chr0027PlaceOrder : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => "orders";
}
```
