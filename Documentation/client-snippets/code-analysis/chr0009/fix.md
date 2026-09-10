```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record Chr0009FixBookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
[EventStore("library")]
public record Chr0009FixBookAdded(string Title);

[EventType("book-borrowed")]
[EventStore("library")]
public record Chr0009FixBookBorrowed(string Title);

// All event types belong to the same event store: "library".
public class Chr0009FixBookStatusReducer : IReducerFor<Chr0009FixBookStatus>
{
    public Chr0009FixBookStatus Reduce(Chr0009FixBookAdded @event, Chr0009FixBookStatus? current, EventContext context) =>
        new(@event.Title, true);

    public Chr0009FixBookStatus Reduce(Chr0009FixBookBorrowed @event, Chr0009FixBookStatus? current, EventContext context) =>
        current is null ? new(string.Empty, false) : current with { IsAvailable = false };
}
```
