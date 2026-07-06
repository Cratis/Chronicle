```csharp
using Cratis.Chronicle.Seeding;

public sealed class EvtSeedingMultipleSameTypeSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<EvtSeedingUserRegistered>("user-123", [
            new("john@example.com", "John"),
            new("jane@example.com", "Jane")
        ]);
    }
}
```
