```csharp
using Cratis.Chronicle.Seeding;

public sealed class EvtSeedingUserSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder
            .For<EvtSeedingUserRegistered>("user-123", [
                new("john@example.com", "John")
            ])
            .ForEventSource("user-456", [
                new EvtSeedingUserRegistered("jane@example.com", "Jane"),
                new EvtSeedingEmailVerified("jane@example.com")
            ]);
    }
}
```
