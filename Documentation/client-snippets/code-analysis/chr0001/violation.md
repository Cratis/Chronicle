```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

// Missing [EventType] attribute
public record Chr0001UserCreated(string Username, string Email);

public class Chr0001UserService(IEventSequence eventSequence)
{
    public Task CreateUser(EventSourceId userId, string username, string email) =>
        // CHR0001: Type 'Chr0001UserCreated' must be marked with [EventType] or [EventTypeGenerationFor<T>] when appended to an event sequence
        eventSequence.Append(userId, new Chr0001UserCreated(username, email));
}
```
