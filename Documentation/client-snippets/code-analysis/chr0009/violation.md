```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record BookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
[EventStore("library")]
public record Chr0009ViolationBookAdded(string Title);

[EventType("book-borrowed")]
[EventStore("rentals")]
public record Chr0009ViolationBookBorrowed(string Title);

// Error CHR0009: Reducer 'Chr0009ViolationBookStatusReducer' reduces event types from
// multiple event stores: "library", "rentals". All event types in a reducer must
// originate from the same event store.
public class Chr0009ViolationBookStatusReducer : IReducerFor<BookStatus>
{
    public BookStatus Reduce(Chr0009ViolationBookAdded @event, BookStatus? current, EventContext context) =>
        new(@event.Title, true);

    public BookStatus Reduce(Chr0009ViolationBookBorrowed @event, BookStatus? current, EventContext context) =>
        current with { IsAvailable = false };
}
```
