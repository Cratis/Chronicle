```csharp
using Cratis.Chronicle.EventSequences;

public class DepositWithRetry(IEventLog eventLog)
{
    public async Task<bool> Deposit(EventSourceId accountId, string month, decimal amount)
    {
        var result = await eventLog.Append<AccountEventSource>(
            accountId,
            new MoneyDeposited(amount),
            eventStream: "Transactions",
            eventStreamId: month);

        if (result.HasConcurrencyViolations)
        {
            // Another writer appended to this month's stream between the tail read and the write.
            // Nothing was appended. Read the new state and decide again.
            return false;
        }

        return result.IsSuccess;
    }
}
```
