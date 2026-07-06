```csharp
using Cratis.Chronicle.Seeding;

public sealed class EvtSeedingMixedTypesSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.ForEventSource("user-123", [
            new EvtSeedingUserRegistered("john@example.com", "John"),
            new EvtSeedingEmailVerified("john@example.com"),
            new EvtSeedingProfileUpdated("John Doe")
        ]);
    }
}
```
