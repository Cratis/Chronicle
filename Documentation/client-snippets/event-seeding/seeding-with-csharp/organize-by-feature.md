```csharp
using Cratis.Chronicle.Seeding;

public sealed class EvtSeedingUserFeatureSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<EvtSeedingUserRegistered>("test-user-1", [
            new("test1@example.com", "Test User 1")
        ]);
    }
}

public sealed class EvtSeedingOrderFeatureSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<EvtSeedingOrderPlaced>("test-order-1", [
            new("test-user-1", 100.00m)
        ]);
    }
}
```
