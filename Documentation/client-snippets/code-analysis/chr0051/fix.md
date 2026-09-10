```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record BookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
public record Chr0051FixBookAdded(string Title);

public class Chr0051FixBookStatusReducer : IReducerFor<BookStatus>
{
    // The current read model parameter is nullable, handling creation correctly.
    public BookStatus Reduce(Chr0051FixBookAdded @event, BookStatus? current, EventContext context) =>
        current ?? new(@event.Title, true);
}
```
