# Appending an event to the event log

The event log is the primary event sequence in Cratis. It is the source of truth we use privately to
derive all other results from.

Appending to it requires you to get a hold of the client representation of the event log and calling
append on it. This is done by taking a dependency to `IEventLog` from the `Cratis.Events` namespace.

Assuming you have an event as defined [see documentation](./creating-an-event.md), you can do the following:

```csharp
using Cratis.Events;

public class DebitAccounts(IEventLog eventLog)
{
    public Task OpenDebitAccount(AccountName Name, CustomerId Owner) =>
        eventLog.Append(Guid.NewGuid().ToString(), new DebitAccountOpened(Name, Owner));
}
```

> Note: The first parameter of `Append()` takes the type `EventSourceId`. This is a wrapper for a string value type.
> The `EventSourceId` is the identifier that identifies the unique instance of a concept in your domain, often referred
> to as an aggregate.

Since Chronicle does not provide a centralized key generator, it is common to use a `Guid` that can be
generated on the fly. This is very common in modern systems, as it removes tension in inserts. The `EventSourceId` type
provides an implicit conversion from `Guid` to `EventSourceId`.
