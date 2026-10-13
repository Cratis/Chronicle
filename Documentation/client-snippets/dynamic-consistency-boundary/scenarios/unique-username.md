```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

[EventType]
public record UserRegistered([property: Unique] string Username, string Email);

public static class UserRegistration
{
    public static async Task<string?> Register(IEventLog eventLog, EventSourceId userId, string username, string email)
    {
        var result = await eventLog.Append(userId, new UserRegistered(username, email));
        if (result.IsSuccess)
        {
            return null;
        }

        // The kernel rejected the append, so this holds for every writer, not only this one.
        return result.HasConstraintViolations
            ? result.ConstraintViolations.First().Message.Value
            : "The user could not be registered.";
    }
}
```
