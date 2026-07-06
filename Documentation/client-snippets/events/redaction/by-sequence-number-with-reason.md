```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class RedactionWithReasonService(IEventLog eventLog)
{
    public Task Redact(EventSequenceNumber sequenceNumber) =>
        eventLog.Redact(sequenceNumber, new RedactionReason("GDPR erasure request"));
}
```
