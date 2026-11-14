```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
public record SubscriptionsExplicitInvoiceIssued(string InvoiceId);

public static class SubscriptionsExplicitSourceNamespaces
{
    public static Task Run(IEventStore eventStore) =>
        eventStore.Subscriptions.Subscribe(
            "invoices-from-billing",
            "billing-service",
            builder => builder
                .WithEventType<SubscriptionsExplicitInvoiceIssued>()
                .WithSourceNamespaces("Default", "Tenant1"));
}
```
