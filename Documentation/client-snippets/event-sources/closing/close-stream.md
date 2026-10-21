```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventSource("Account")]
[EventStream("Transactions")]
public class AccountEventSource : IEventSource;

public class MonthEnd(IEventStore eventStore)
{
    public async Task<bool> CloseMonth(EventSourceId accountId, string month)
    {
        // Take the names from the definition, so a rename cannot leave a closure behind with the old name.
        var definition = eventStore.EventSources.GetFor(typeof(AccountEventSource));
        var transactions = definition.FindStream("Transactions")!;

        var scope = new ClosedStreamScope(
            EventSourceId: accountId,
            EventSourceType: definition.EventSourceType,
            EventStreamType: transactions.EventStreamType,
            EventStreamId: month);

        var closed = false;
        var result = await eventStore.EventLog.CompleteStream(scope);
        result.Switch(
            _ => closed = true,
            error => closed = error == CompleteStreamError.AlreadyCompleted);
        return closed;
    }
}
```
