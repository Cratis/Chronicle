```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record RedactionPersonalDetailsRecorded(string Name, string SocialSecurityNumber);

[EventType]
public record RedactionAddressChanged(string Street, string City);

public class RedactionByEventSourceAndTypesService(IEventLog eventLog)
{
    public Task RedactPersonalData(EventSourceId eventSourceId) =>
        eventLog.Redact(
            eventSourceId,
            new RedactionReason("PII erasure"),
            typeof(RedactionPersonalDetailsRecorded),
            typeof(RedactionAddressChanged));
}
```
