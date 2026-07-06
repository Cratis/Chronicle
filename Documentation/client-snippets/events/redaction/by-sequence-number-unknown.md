```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public class RedactionUnknownReasonService(IEventLog eventLog)
{
    public Task Redact(EventSequenceNumber sequenceNumber) =>
        eventLog.Redact(sequenceNumber, RedactionReason.Unknown);
}
```
