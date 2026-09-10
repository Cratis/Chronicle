```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record BookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
public record Chr0051ViolationBookAdded(string Title);

public class Chr0051ViolationBookStatusReducer : IReducerFor<BookStatus>
{
    // Warning CHR0051: Reducer method 'Reduce' declares its current read model parameter
    // as 'BookStatus' rather than 'BookStatus?'
    public BookStatus Reduce(Chr0051ViolationBookAdded @event, BookStatus current, EventContext context) =>
        new(@event.Title, true);
}
```
