# Appending an event to the event log

The event log is the primary event sequence in Cratis. It is the source of truth we use privately to
derive all other results from.

Appending to it requires you to get a hold of the client representation of the event log and calling
append on it. This is done by taking a dependency to `IEventLog` from the `Aksio.Cratis.Events` namespace.

Assuming you have an event as defined [here](./creating-an-event.md), you can do the following:

```csharp
using Aksio.Cratis.Events;

public class DebitAccounts
{
    readonly IEventLog _eventLog;

    public DebitAccounts(IEventLog eventLog) => _eventLog = eventLog;

    public Task OpenDebitAccount(AccountName Name, PersonId Owner) => _eventLog.Append(Guid.NewGuid().ToString(), new DebitAccountOpened(Name, Owner));
}
```

> Note: The first parameter of `Append()` takes the type `EventSourceId`. This is a wrapper for a string value type.
> The `EventSourceId` is the identifier that identifies the unique instance of a concept in your domain, often referred
> to as an aggregate.

The choice of using a **Guid** as the unique identifier is that there is no centralized primary key sequence that
can give you unique keys. A generated **Guid** can however be generated anywhere and will be unique.
