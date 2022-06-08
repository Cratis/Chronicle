# Appending an event to the outbox

It is possible to append directly to the outbox. However this can become hard to maintain or complex
to get right, so recommend looking at [projections](./projecting-to-outbox.md) for the same purpose.

If appending directly is the correct approach for your use case, it is very similar to how
the [appending to event log](./appending-an-event-to-event-log.md) works. You can do this anywhere you
can take a dependency to the `IEventOutbox` type from the `Aksio.Cratis.Events` namespace.
For instance, that could be directly in your domain or in an observer that is reacting to private events.
The latter would make it more decouple and is probably preferred from a systems maintenance perspective.

Below is an example of

```csharp
using Aksio.Cratis.Events;

public class DebitAccountsBalance
{
    readonly IEventOutbox _eventOutbox;

    public DebitAccounts(IEventOutbox eventOutbox) => _eventOutbox = eventOutbox;

    public Task BalanceChanged(AccountId accountId, double amount) => _eventOutbox.Append(context.EventSourceId, new AccountBalance(Name, Owner));
}
```
