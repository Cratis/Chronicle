```csharp
using Cratis.Chronicle.EventSequences;

public class AccountOpener(IEventLog eventLog)
{
    // No stream: the event is written with the event source type "Account" and the default stream.
    public Task<AppendResult> Open(EventSourceId accountId, string owner) =>
        eventLog.Append<AccountEventSource>(accountId, new AccountOpened(owner));
}
```
