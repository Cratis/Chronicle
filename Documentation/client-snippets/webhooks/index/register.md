```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Webhooks;

[EventType]
public record WebhooksIndexAccountOpened(string OwnerName);

public class WebhooksIndexRegister(IEventStore eventStore)
{
    public async Task RegisterWebhook() =>
        await eventStore.Webhooks.Register(
            "account-events",
            "https://example.com/chronicle/webhooks",
            builder => builder
                .WithEventType<WebhooksIndexAccountOpened>()
                .WithHeader("x-source", "my-app")
                .WithBearerToken(Environment.GetEnvironmentVariable("WEBHOOK_TOKEN")!));
}
```
