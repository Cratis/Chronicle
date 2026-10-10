```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public class InspectingClosedStreams(IEventLog eventLog)
{
    public async Task Print(EventSourceId reportId)
    {
        // Everything closed for one event source. Omit the argument to list every closure.
        var closedStreams = await eventLog.GetClosedStreams(ClosedStreamScope.ForEventSource(reportId));

        foreach (var closed in closedStreams)
        {
            var owner = closed.Origin == ClosedStreamOrigin.ClosingEvent
                ? $"closing event constraint '{closed.ClosedBy}'"
                : "manual CompleteStream";
            Console.WriteLine($"{closed.Scope} closed at {closed.SequenceNumber} ({closed.ClosedAt}) by {owner}");
        }
    }
}
```
