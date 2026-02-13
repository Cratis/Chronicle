# Appending

Appending adds a single event to an event sequence. The event log is the default sequence, but you can also append to any custom sequence your system defines.

## How it works

When you append an event, Chronicle:

- Validates the event type and captures metadata
- Applies any [cross-cutting properties](cross-cutting-properties.md)
- Associates the event with its event source
- Assigns the next sequence number
- Persists the event and metadata atomically
- Updates the sequence state

The append returns an operation result you can use to detect concurrency violations or other failures. This makes it safe to retry or compensate without duplicating events.

## AppendResult

Appending returns an `AppendResult` that describes whether the operation succeeded and includes details about what went wrong when it fails. It can surface concurrency violations, constraint violations, and other append-time problems. See [Concurrency](concurrency.md) and [Constraints](../constraints/index.md) for more on these cases.

## When to use

Appending is lightweight and intended for single domain decisions that produce one event. When your decision spans multiple event sources, use concurrency scopes to enforce the boundary while keeping sequences independent. See [Concurrency](concurrency.md) for details.

## Example

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record OrderPlaced(string CustomerId, decimal Total);

public class CheckoutService(IEventLog eventLog)
{
    public async Task PlaceOrder(OrderId orderId, string customerId, decimal total)
    {
        var result = await eventLog.Append(
            orderId,
            new OrderPlaced(customerId, total)
        );

        if (!result.IsSuccess)
        {
            // Decide whether to retry or surface a conflict to the caller.
        }
    }
}
```

