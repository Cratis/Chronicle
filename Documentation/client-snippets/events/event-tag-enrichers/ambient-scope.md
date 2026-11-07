```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
public record EnrichersOrderPlaced(string OrderId);

public static class EnrichersAmbientScope
{
    public static async Task Place(IEventStore eventStore)
    {
        // Every event appended inside the scope gets region=eu, without the append code knowing.
        using var scope = EventTags.Scope("region", "eu");
        await eventStore.EventLog.Append("order-1", new EnrichersOrderPlaced("order-1"));
    }
}
```
