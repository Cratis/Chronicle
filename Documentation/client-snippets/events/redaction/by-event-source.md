```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class RedactionByEventSourceService(IEventLog eventLog)
{
    public Task RedactAccount(EventSourceId eventSourceId) =>
        eventLog.Redact(eventSourceId, new RedactionReason("Account deletion requested"));
}
```
