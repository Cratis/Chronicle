```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Seeding;

public static class EvtSeedingCorrection
{
    public static async Task Register(IEventStore eventStore)
    {
        var correctedSeeding = eventStore.CreateEventSeeding();
        correctedSeeding.For<EvtSeedingUserRegistered>("user-123", [
            new("john@example.com", "John Doe")
        ]);

        await correctedSeeding.Register();
    }
}
```
