# Appending many

Appending many events in a single call is useful for batch workflows such as imports, migrations, or multi-step business operations.

> [!NOTE]
> With Cratis Arc and the Chronicle extension, you can append events as state changes coming out of a command using a more functional approach. See <xref:Arc.Chronicle.Commands>.

Batch appends preserve ordering and reduce per-event overhead compared to appending each event individually.

AppendMany is transactional all the way to storage. Either all events in the batch are committed, or none are. Chronicle assigns sequence numbers in order, persists the events and metadata atomically, and updates the sequence state once the batch succeeds.

## How it works

- Each item in the batch pairs an event with the event source it belongs to.
- The batch is validated as a unit and written atomically.
- Sequence numbers are assigned in the order provided.
- The event sequence state is updated once the batch commits.

## When to use

Use this approach when you already have a set of events that should be appended together. Each event can still target different event sources, and you can include concurrency scopes to validate the boundary across streams. See [Concurrency](concurrency.md) for details.

## Example

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

        return eventLog.AppendMany(events);
    }
}
```

