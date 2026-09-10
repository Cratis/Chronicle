```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record BookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
[EventStore("library")]
public record Chr0009FixBookAdded(string Title);

[EventType("book-borrowed")]
[EventStore("library")]
public record Chr0009FixBookBorrowed(string Title);

// All event types belong to the same event store: "library".
public class Chr0009FixBookStatusReducer : IReducerFor<BookStatus>
{
    public BookStatus Reduce(Chr0009FixBookAdded @event, BookStatus? current, EventContext context) =>
        new(@event.Title, true);

    public BookStatus Reduce(Chr0009FixBookBorrowed @event, BookStatus? current, EventContext context) =>
        current with { IsAvailable = false };
}
```
