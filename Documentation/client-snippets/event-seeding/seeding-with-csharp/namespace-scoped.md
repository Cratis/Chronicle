```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Seeding;

[EventType]
public record EvtSeedingProductCreated(string Name, decimal Price);

[EventType]
public record EvtSeedingOrganizationCreated(string Name);

[EventType]
public record EvtSeedingBillingSetUp(string BillingEmail);

public sealed class EvtSeedingTenantSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        // Global seed data — applied to every namespace
        builder.For<EvtSeedingProductCreated>("product-1", [
            new("Laptop", 1299.00m)
        ]);

        // Namespace-scoped seed data — applied only to the "acme" namespace
        builder.ForNamespace("acme")
            .For<EvtSeedingUserRegistered>("user-1", [
                new("admin@acme.com", "Acme Admin")
            ]);

        // A second namespace with different seed data
        builder.ForNamespace("contoso")
            .For<EvtSeedingUserRegistered>("user-1", [
                new("admin@contoso.com", "Contoso Admin")
            ])
            .ForEventSource("org-1", [
                new EvtSeedingOrganizationCreated("Contoso"),
                new EvtSeedingBillingSetUp("contoso@billing.com")
            ]);
    }
}
```
