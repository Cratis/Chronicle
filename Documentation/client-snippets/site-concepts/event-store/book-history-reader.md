```csharp
using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class BookHistoryReader(IEventLog eventLog)
{
    public Task<IImmutableList<AppendedEvent>> GetBorrowings(EventSourceId bookId) =>
        eventLog.GetForEventSourceIdAndEventTypes(
            bookId,
            [typeof(BookBorrowed).GetEventType()]);
}
```
