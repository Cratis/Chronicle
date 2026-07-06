```csharp
using Cratis.Chronicle.Events;

public static class EventSourceIdConversionExamples
{
    public static void FromGuid()
    {
        // From a Guid — common for aggregate-style identifiers
        EventSourceId id = Guid.NewGuid();
    }

    public static void FromString()
    {
        // From a string — useful for natural keys
        EventSourceId id = "order-42";
    }

    public static void Generated()
    {
        // Generate a new random identifier
        EventSourceId id = EventSourceId.New();
    }
}
```
