```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventType]
public record MoneyDeposited(decimal Amount);

[EventSource("Account", Description = "A customer's bank account", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Onboarding", Description = "Opening the account and verifying the customer")]
[EventStream(
    "Transactions",
    Description = "Deposits and withdrawals, one stream per month",
    Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class AccountEventSource : IEventSource;

public static class Deposits
{
    public static Task<AppendResult> Deposit(IEventLog eventLog, EventSourceId accountId, decimal amount, string month) =>
        eventLog.Append<AccountEventSource>(
            accountId,
            new MoneyDeposited(amount),
            eventStream: "Transactions",
            eventStreamId: month);
}
```
