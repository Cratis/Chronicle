```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventType]
public record MoneyDeposited(decimal Amount);

[EventType]
public record MoneyWithdrawn(decimal Amount);

// Every event of an account conflicts with every other event of the same account, except in the
// Transactions stream, where only events of the same month conflict.
[EventSource("Account", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Onboarding")]
[EventStream(
    "Transactions",
    Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class AccountEventSource : IEventSource;

public class MonthlyDeposits(IEventLog eventLog)
{
    // No scope is passed, so Chronicle builds one from the dimensions: it reads the tail of this
    // account's Transactions stream for this month and expects it to be unchanged at append time.
    public async Task<bool> Deposit(EventSourceId accountId, string month, decimal amount)
    {
        var result = await eventLog.Append<AccountEventSource>(
            accountId,
            new MoneyDeposited(amount),
            eventStream: "Transactions",
            eventStreamId: month);

        return result.IsSuccess;
    }
}
```
