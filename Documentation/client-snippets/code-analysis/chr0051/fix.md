```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record Chr0051FixBookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
public record Chr0051FixBookAdded(string Title);

public class Chr0051FixBookStatusReducer : IReducerFor<Chr0051FixBookStatus>
{
    // The current read model parameter is nullable, handling creation correctly.
    public Chr0051FixBookStatus Reduce(Chr0051FixBookAdded @event, Chr0051FixBookStatus? current, EventContext context) =>
        current ?? new(@event.Title, true);
}
```
