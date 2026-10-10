```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

[EventType]
public record ScopedClosingReportApproved(string Approver);

public class ScopedClosingApprover(IEventLog eventLog)
{
    public async Task<bool> ApproveAndClose(EventSourceId reportId)
    {
        var scope = new ClosedStreamScope(
            EventSourceId: reportId,
            EventStreamType: new EventStreamType("monthly-report"),
            EventStreamId: new EventStreamId("2026-04"));

        var approved = await eventLog.Append(
            reportId,
            new ScopedClosingReportApproved("alice"),
            new EventStreamType("monthly-report"),
            new EventStreamId("2026-04"));
        if (!approved.IsSuccess) return false;

        // Close only if nothing has landed in this scope after the approval.
        var result = await eventLog.CompleteStream(scope, expectedTailSequenceNumber: approved.SequenceNumber);

        var closed = false;
        result.Switch(
            _ => closed = true,
            error => closed = error == CompleteStreamError.AlreadyCompleted);
        return closed;
    }
}
```
