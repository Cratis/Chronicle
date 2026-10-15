```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

[EventType]
[ClosesStream(EventStreamIdFrom = nameof(Period))]
public record CloseTheBooksBooksClosed(string Period, decimal ClosingBalance);

[EventType]
public record CloseTheBooksPeriodOpened(string Period, decimal OpeningBalance);

public class CloseTheBooksLedger(IEventLog eventLog)
{
    static readonly EventStreamType _ledger = new("ledger");

    public async Task<AppendResult> CloseApril(EventSourceId ledgerId, decimal closingBalance)
    {
        // The closing fact closes (ledger, "ledger", "2026-04") in the same step as the append.
        var closing = await eventLog.Append(
            ledgerId,
            new CloseTheBooksBooksClosed("2026-04", closingBalance),
            _ledger,
            new EventStreamId("2026-04"));
        if (!closing.IsSuccess) return closing;

        // The application chooses the next stream id and records the carry-over as its first fact.
        return await eventLog.Append(
            ledgerId,
            new CloseTheBooksPeriodOpened("2026-05", closingBalance),
            _ledger,
            new EventStreamId("2026-05"));
    }
}
```
