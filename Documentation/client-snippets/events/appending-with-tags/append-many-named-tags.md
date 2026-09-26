```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record NamedTaggedMoneyWithdrawn(decimal Amount);

[EventType]
public record NamedTaggedMoneyDeposited(decimal Amount);

public class NamedTaggedTransferService(IEventLog eventLog)
{
    public Task<AppendManyResult> Transfer(EventSourceId fromAccount, EventSourceId toAccount, decimal amount, string transferId)
    {
        EventForEventSourceId[] events =
        [
            new(fromAccount, new NamedTaggedMoneyWithdrawn(amount)) { NamedTags = [new NamedTag("ledger-side", "debit")] },
            new(toAccount, new NamedTaggedMoneyDeposited(amount)) { NamedTags = [new NamedTag("ledger-side", "credit")] }
        ];

        // The withdrawal carries ledger-side = debit and transfer = <transferId>.
        // The deposit carries ledger-side = credit and transfer = <transferId>.
        return eventLog.AppendMany(
            events,
            namedTags: [new NamedTag("transfer", transferId)],
            tags: ["transfer"]);
    }
}
```
