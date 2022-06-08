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

    public Task OpenDebitAccount(AccountName Name, PersonId Owner) => _eventLog.Append(new DebitAccountOpened(Name, Owner));
}
```
