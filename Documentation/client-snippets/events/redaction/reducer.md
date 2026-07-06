```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

[EventType]
public record RedactionReducerPersonRegistered(string Name);

public record RedactionPersonReadModel(string Name);

public class RedactionPersonReadModelReducer : IReducerFor<RedactionPersonReadModel>
{
    public RedactionPersonReadModel Registered(RedactionReducerPersonRegistered @event, RedactionPersonReadModel? current, EventContext context) =>
        (current ?? new RedactionPersonReadModel(string.Empty)) with
        {
            Name = @event.Name
        };

    public RedactionPersonReadModel? Redacted(EventRedacted @event, RedactionPersonReadModel? current, EventContext context) =>
        // Return null to remove the read model, or return a sanitised version
        null;
}
```
