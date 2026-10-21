```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record BookBorrowed(string Member, DateOnly DueDate);

public class LendingService(IEventLog eventLog)
{
    public Task Lend(EventSourceId bookId, string member, DateOnly dueDate) =>
        eventLog.Append(bookId, new BookBorrowed(member, dueDate));
}
```
