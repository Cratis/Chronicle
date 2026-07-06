```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class GettingStateCheckpointStore(IEventLog eventLog)
{
    public async Task<EventSequenceNumber> CaptureTail()
    {
        // Persists the current tail so processing can resume later.
        return await eventLog.GetTailSequenceNumber();
    }
}
```
