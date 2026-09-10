```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record Chr0009ViolationBookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
[EventStore("library")]
public record Chr0009ViolationBookAdded(string Title);

[EventType("book-borrowed")]
[EventStore("rentals")]
public record Chr0009ViolationBookBorrowed(string Title);

// Error CHR0009: Reducer 'Chr0009ViolationBookStatusReducer' reduces event types from
// multiple event stores: "library", "rentals". All event types in a reducer must
// originate from the same event store.
public class Chr0009ViolationBookStatusReducer : IReducerFor<Chr0009ViolationBookStatus>
{
    public Chr0009ViolationBookStatus Reduce(Chr0009ViolationBookAdded @event, Chr0009ViolationBookStatus? current, EventContext context) =>
        new(@event.Title, true);

    public Chr0009ViolationBookStatus Reduce(Chr0009ViolationBookBorrowed @event, Chr0009ViolationBookStatus? current, EventContext context) =>
        current is null ? new(string.Empty, false) : current with { IsAvailable = false };
}
```
