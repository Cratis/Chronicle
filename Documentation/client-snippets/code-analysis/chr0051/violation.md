```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record Chr0051ViolationBookStatus(string Title, bool IsAvailable);

[EventType("book-added")]
public record Chr0051ViolationBookAdded(string Title);

public class Chr0051ViolationBookStatusReducer : IReducerFor<Chr0051ViolationBookStatus>
{
    // Warning CHR0051: Reducer method 'Reduce' declares its current read model parameter
    // as 'Chr0051ViolationBookStatus' rather than 'Chr0051ViolationBookStatus?'
    public Chr0051ViolationBookStatus Reduce(Chr0051ViolationBookAdded @event, Chr0051ViolationBookStatus current, EventContext context) =>
        new(@event.Title, true);
}
```
