```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventType]
public record AccountOpened(string Owner);

[EventType]
public record IdentityVerified(string Method);

[EventType]
public record MoneyDeposited(decimal Amount);

[EventType]
public record MoneyWithdrawn(decimal Amount);

[EventSource("Account", Description = "A customer's bank account", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Onboarding", Description = "Opening the account and verifying the customer")]
[EventStream(
    "Transactions",
    Description = "Deposits and withdrawals, one stream per month",
    Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class AccountEventSource : IEventSource;

public class AccountService(IEventLog eventLog)
{
    public Task<AppendResult> Open(EventSourceId accountId, string owner) =>
        eventLog.Append<AccountEventSource>(accountId, new AccountOpened(owner), eventStream: "Onboarding");

    public Task<AppendResult> Verify(EventSourceId accountId, string method) =>
        eventLog.Append<AccountEventSource>(accountId, new IdentityVerified(method), eventStream: "Onboarding");

    public Task<AppendResult> Deposit(EventSourceId accountId, string month, decimal amount) =>
        eventLog.Append<AccountEventSource>(
            accountId,
            new MoneyDeposited(amount),
            eventStream: "Transactions",
            eventStreamId: month);
}
```
