```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class TaggingUserLoginService(IEventLog eventLog)
{
    public Task RecordLogin(EventSourceId eventSourceId) =>
        // The event will end up with four tags: ["analytics", "user-action", "production", "critical"]
        eventLog.Append(
            eventSourceId,
            new TaggingUserLoggedIn("user123", DateTimeOffset.UtcNow),
            tags: ["production", "critical"]);
}
```
