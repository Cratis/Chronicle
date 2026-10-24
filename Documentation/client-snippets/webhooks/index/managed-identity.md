```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

[EventType]
public record WebhooksIndexManagedIdentityAccountOpened(string OwnerName);

public class WebhooksIndexManagedIdentity(IEventStore eventStore)
{
    public async Task RegisterWebhook() =>
        await eventStore.Webhooks.Register(
            "account-events-managed-identity",
            "https://example.com/chronicle/webhooks",
            builder => builder
                .WithEventType<WebhooksIndexManagedIdentityAccountOpened>()
                .WithManagedIdentity("api://<receiver-application-id>/.default"));
}
```
