```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record AccountOpened(string Name);

[EventType]
public record AccountRenamed(string Name);

public class CopyAccountOpeningsService(IEventLog eventLog)
{
    public Task<Guid> CopyAccountOpenings(EventSourceId sourceId, EventSourceId targetId) =>
        eventLog.Copy(sourceId, targetId, typeof(AccountOpened), typeof(AccountRenamed));
}
```
