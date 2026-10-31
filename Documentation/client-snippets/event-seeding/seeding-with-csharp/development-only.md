```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Seeding;

#if DEBUG
public sealed class EvtSeedingDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.ForNamespace(EventStoreNamespaceName.Default).For<EvtSeedingUserRegistered>("dev-user-1", [
            new("dev@example.com", "Dev User")
        ]);
    }
}
#endif
```
