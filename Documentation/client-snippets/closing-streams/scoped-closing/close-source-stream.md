```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public class ScopedClosingReportCloser(IEventLog eventLog)
{
    public async Task CloseApprovedReport(EventSourceId reportId)
    {
        // Close one report's "2026-04" stream. Other reports that use the same
        // stream type and stream id stay open.
        var scope = new ClosedStreamScope(
            EventSourceId: reportId,
            EventStreamType: new EventStreamType("monthly-report"),
            EventStreamId: new EventStreamId("2026-04"));

        var result = await eventLog.CompleteStream(scope);

        result.Switch(
            sequenceNumber => Console.WriteLine($"Closed at sequence number {sequenceNumber}"),
            error => Console.WriteLine($"Not closed: {error}"));
    }
}
```
