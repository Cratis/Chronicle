# Appending

Appending adds a single event to an event sequence. The event log is the default sequence, but you can also append to any custom sequence your system defines.

> [!NOTE]
> With Cratis Arc and the Chronicle extension, you can append events as state changes coming out of a command using a more functional approach. See <xref:Arc.Chronicle.Commands>.

## How it works

When you append an event, Chronicle:

- Validates the event content against its registered JSON schema
- Applies any [cross-cutting properties](cross-cutting-properties.md)
- Associates the event with its event source
- Assigns the next sequence number
- Persists the event and metadata atomically
- Updates the sequence state

The append returns an operation result you can use to detect schema errors, concurrency violations, constraint violations, and other append-time problems. This makes it safe to retry or revise without duplicating events.

## AppendResult

Appending returns an `AppendResult` that describes whether the operation succeeded and includes details about what went wrong when it fails. It can surface schema validation errors, concurrency violations, constraint violations, and other append-time problems. See [Concurrency](concurrency.md) and [Constraints](../constraints/index.md) for more on these cases.

## Schema validation

Chronicle validates every event's content against its registered JSON schema before persisting it. If the content does not conform to the schema — for example, a required property is missing or has the wrong type — the append fails and the errors appear in `AppendResult.Errors`.

```csharp
var result = await eventLog.Append(eventSourceId, new OrderPlaced(customerId, total));

if (result.HasErrors)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Schema error: {error}");
    }
}
```

Schema validation runs before any other checks, so a schema error is returned immediately without touching the event sequence.

## Specifying when an event occurred

By default, Chronicle assigns the current server time as the event's `Occurred` timestamp when it persists the event. You can override this by passing an explicit `DateTimeOffset` to the `occurred` parameter.

```csharp
var result = await eventLog.Append(
    eventSourceId,
    new OrderPlaced(customerId, total),
    occurred: DateTimeOffset.Parse("2024-01-15T10:30:00Z")
);
```

> [!WARNING]
> Specifying `occurred` bypasses the automatic server timestamp. Use this only for importing historical events or replaying data from an external system. Incorrect timestamps can break projections and reactors that rely on event ordering by time.

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

