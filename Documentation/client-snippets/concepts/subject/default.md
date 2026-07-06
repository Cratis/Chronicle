```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
public record SubjectAuthorRegistered(string Name);

public class SubjectAuthorService(IEventStore eventStore)
{
    public Task Register(EventSourceId authorId, string name) =>
        // Subject defaults to authorId; encryption keys for any PII on SubjectAuthorRegistered
        // are keyed by authorId.
        eventStore.EventLog.Append(authorId, new SubjectAuthorRegistered(name));
}
```
