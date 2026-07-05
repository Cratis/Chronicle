```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record RedactionPersonRegistered(string Name);

public class RedactionPersonReactor : IReactor
{
    public Task Registered(RedactionPersonRegistered @event, EventContext context)
    {
        // Handle the original event
        return Task.CompletedTask;
    }

    public Task Redacted(EventRedacted @event, EventContext context)
    {
        // Called only when a RedactionPersonRegistered event (a type this reactor handles) is redacted.
        // @event.OriginalEventType is typeof(RedactionPersonRegistered).
        // Use this to undo any side effects produced by the original event.
        return Task.CompletedTask;
    }
}
```
