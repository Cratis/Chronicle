```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

[EventType]
public record CloseTheBooksEntryPosted(string Period, decimal Amount);

public interface ICloseTheBooksCurrentPeriod
{
    Task<string> For(EventSourceId ledgerId);
}

public class CloseTheBooksPosting(IEventLog eventLog, ICloseTheBooksCurrentPeriod currentPeriod)
{
    public async Task<AppendResult> Post(EventSourceId ledgerId, decimal amount)
    {
        AppendResult result;
        var attempts = 0;

        do
        {
            // The current period comes from a read model of the open instance.
            var period = await currentPeriod.For(ledgerId);
            result = await eventLog.Append(
                ledgerId,
                new CloseTheBooksEntryPosted(period, amount),
                new EventStreamType("ledger"),
                new EventStreamId(period));
            attempts++;
        }
        while (IsClosed(result) && attempts < 3);

        return result;
    }

    static bool IsClosed(AppendResult result) =>
        result.ConstraintViolations.Any(violation => violation.ConstraintType == ConstraintType.StreamClosed);
}
```
