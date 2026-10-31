```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ClosingEventsEntryPosted(string Description);

public class ClosingEventsAfterClose(IEventLog eventLog)
{
    public async Task<bool> Run(EventSourceId ledgerId)
    {
        await eventLog.Append(
            ledgerId,
            new ClosingEventsBooksClosed("2026-04", 1200m),
            new EventStreamType("ledger"),
            new EventStreamId("2026-04"));

        var late = await eventLog.Append(
            ledgerId,
            new ClosingEventsEntryPosted("Late invoice"),
            new EventStreamType("ledger"),
            new EventStreamId("2026-04"));

        // The late entry is refused; the next period is unaffected.
        return late.ConstraintViolations.Any(violation => violation.ConstraintType == ConstraintType.StreamClosed);
    }
}
```
