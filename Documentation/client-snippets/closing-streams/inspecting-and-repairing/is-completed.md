```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public class InspectingIsCompleted(IEventLog eventLog)
{
    public async Task<bool> IsApril(EventSourceId reportId) =>
        await eventLog.IsStreamCompleted(new ClosedStreamScope(
            EventSourceId: reportId,
            EventStreamType: new EventStreamType("monthly-report"),
            EventStreamId: new EventStreamId("2026-04")));
}
```
