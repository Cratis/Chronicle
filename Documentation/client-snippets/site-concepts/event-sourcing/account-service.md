```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

public class AccountService(IEventLog eventLog)
{
    public async Task Withdraw(EventSourceId accountId, decimal amount)
    {
        var result = await eventLog.Append(accountId, new WithdrawalMade(amount));

        if (!result.IsSuccess)
        {
            // Decide whether to retry or surface a conflict to the caller.
        }
    }
}
```
