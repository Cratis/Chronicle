```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
[Public]
[Audience]
public record AudiencesRegistrationCompleted(string Organization);

public static class AudiencesRequired
{
    public static async Task Complete(IEventStore eventStore)
    {
        var outbox = eventStore.GetEventSequence(Cratis.Chronicle.EventSequences.EventSequenceId.Outbox);

        // Throws MissingAudience: the event type requires an audience and none was provided.
        // await outbox.Append("org-1", new AudiencesRegistrationCompleted("Acme"));

        using var scope = EventTags.Audience("lobby-a");
        await outbox.Append("org-1", new AudiencesRegistrationCompleted("Acme"));
    }
}
```
