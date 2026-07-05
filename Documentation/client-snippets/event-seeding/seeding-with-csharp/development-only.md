```csharp
using Cratis.Chronicle.Seeding;

#if DEBUG
public sealed class EvtSeedingDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<EvtSeedingUserRegistered>("dev-user-1", [
            new("dev@example.com", "Dev User")
        ]);
    }
}
#endif
```
