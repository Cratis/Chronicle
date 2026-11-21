```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Webhooks;

public class WebhookSignatureVerifierFactory(IEventStore eventStore)
{
    public async Task<IWebhookSignatureVerifier> Create()
    {
        var keys = await eventStore.Webhooks.GetSigningKeys();
        return new WebhookSignatureVerifier(keys);
    }
}
```
