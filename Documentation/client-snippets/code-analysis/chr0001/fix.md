```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record Chr0001UserCreatedFixed(string Username, string Email);

public class Chr0001UserServiceFixed(IEventSequence eventSequence)
{
    public Task CreateUser(EventSourceId userId, string username, string email) =>
        // Now valid
        eventSequence.Append(userId, new Chr0001UserCreatedFixed(username, email));
}
```
