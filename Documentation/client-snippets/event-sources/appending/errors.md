```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

public class CarefulDeposits(IEventLog eventLog)
{
    public async Task<string> Deposit(EventSourceId accountId, decimal amount, string stream, string month)
    {
        try
        {
            var result = await eventLog.Append<AccountEventSource>(
                accountId,
                new MoneyDeposited(amount),
                eventStream: stream,
                eventStreamId: month);

            return result.IsSuccess ? "Deposited" : "Rejected by the kernel";
        }
        catch (EventStreamDoesNotBelongToEventSource)
        {
            // The stream is not one of the streams the definition declares. Nothing was sent.
            return $"'{stream}' is not a stream of the Account event source";
        }
        catch (UnknownEventSource)
        {
            // The type is not a discovered definition. Nothing was sent.
            return "AccountEventSource is not discovered";
        }
    }
}
```
