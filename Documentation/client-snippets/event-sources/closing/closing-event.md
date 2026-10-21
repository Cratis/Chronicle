```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

[EventType]
public record MoneyDeposited(decimal Amount);

// Appending this event closes the scope it is appended to: its event source id, stream type and stream id.
[EventType]
[ClosesStream]
public record MonthClosed(decimal ClosingBalance);

// AccountEventSource is the definition from the previous example.
public class MonthEndWithAnEvent(IEventLog eventLog)
{
    public async Task<bool> CloseAndTryAgain(EventSourceId accountId, string month, decimal balance)
    {
        await eventLog.Append<AccountEventSource>(
            accountId,
            new MonthClosed(balance),
            eventStream: "Transactions",
            eventStreamId: month);

        var late = await eventLog.Append<AccountEventSource>(
            accountId,
            new MoneyDeposited(10m),
            eventStream: "Transactions",
            eventStreamId: month);

        // The late deposit is refused. The next month's stream is unaffected.
        return late.ConstraintViolations.Any(violation => violation.ConstraintType == ConstraintType.StreamClosed);
    }
}
```
