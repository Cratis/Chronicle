# Appending with Tags

Chronicle lets you associate tags with appended events. Tags are stored as event metadata and can be used for categorization, filtering, and concurrency scoping.

## Built-in metadata tags

Chronicle maintains built-in metadata tags for event identity and stream routing:

- `EventSourceType`
- `EventSourceId`
- `EventStreamType`
- `EventStreamId`

You can add custom tags in addition to these built-in tags. For the full list and behavior, see [Event Metadata Tags](../concepts/event-metadata-tags.md).

## Custom tags

Custom tags are simple strings that you provide when appending. They are merged with any static tags defined on the event type.

## Append with tags

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record OrderPlaced(string CustomerId, decimal Total);

public class CheckoutService(IEventLog eventLog)
{
    public Task PlaceOrder(OrderId orderId, string customerId, decimal total)
    {
        return eventLog.Append(
            orderId,
            new OrderPlaced(customerId, total),
            tags: ["checkout", "priority"]);
    }
}
```

## AppendMany with tags

`AppendMany` applies the provided tags to each event in the batch.

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record MoneyWithdrawn(decimal Amount);

[EventType]
public record MoneyDeposited(decimal Amount);

public class TransferService(IEventLog eventLog)
{
    public Task Transfer(AccountId fromAccount, AccountId toAccount, decimal amount)
    {
        var events = new[]
        {
            new EventForEventSourceId(fromAccount, new MoneyWithdrawn(amount)),
            new EventForEventSourceId(toAccount, new MoneyDeposited(amount))
        };

        return eventLog.AppendMany(events, tags: ["transfer", "audit"]);
    }
}
```

