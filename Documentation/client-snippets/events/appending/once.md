```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

[EventType]
public record OrderPlacedOnce(string CustomerId, decimal Total);

public class OnceCheckoutService(IEventLog eventLog)
{
    public async Task PlaceOrder(string orderId, string customerId, decimal total)
    {
        if (!eventLog.SupportsIdempotentAppends)
        {
            // The connected kernel is too old; a keyed append would throw IdempotentAppendsNotSupported.
            return;
        }

        // The key comes from the business operation, so a retry builds the same key.
        var key = new IdempotencyKey($"place-order:{orderId}");

        var result = await eventLog.AppendOnce(
            orderId,
            new OrderPlacedOnce(customerId, total),
            key);

        if (result.HasIdempotencyConflicts)
        {
            // The key already identifies a different event. Do not retry with it.
            var conflict = result.IdempotencyConflicts.First();
            Console.WriteLine($"{conflict.Key} is held by #{conflict.ExistingSequenceNumber}: {conflict.Reason}");
            return;
        }

        switch (result.Outcome)
        {
            case AppendOutcome.Appended:
                break; // This call wrote the event.
            case AppendOutcome.AlreadyAppended:
            case AppendOutcome.AlreadyAppendedAfterErasure:
                break; // An earlier attempt already wrote it; nothing was written now.
        }
    }
}
```
