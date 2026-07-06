```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ConcurrencyPaymentProcessed(decimal Amount);

[EventType]
public record ConcurrencyPaymentFailed(decimal Amount);

[EventType]
public record ConcurrencyPaymentRefunded(decimal Amount);

public class ConcurrencyAccountService(IEventLog eventLog)
{
    public async Task ProcessPayment(EventSourceId accountId, decimal amount)
    {
        // Only check concurrency for payment-related events
        var concurrencyScope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(accountId)
            .WithSequenceNumber(20)
            .WithEventType<ConcurrencyPaymentProcessed>()
            .WithEventType<ConcurrencyPaymentFailed>()
            .WithEventType<ConcurrencyPaymentRefunded>()
            .Build();

        await eventLog.Append(
            accountId,
            new ConcurrencyPaymentProcessed(amount),
            concurrencyScope: concurrencyScope);
    }
}
```
