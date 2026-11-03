```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class CopyAccountEventsService(IEventLog eventLog)
{
    public Task<Guid> CopyAccountOpenings(EventSourceId sourceId, EventSourceId targetId) =>
        eventLog.Copy(sourceId, targetId, typeof(AccountOpened), typeof(AccountRenamed));
}
```
