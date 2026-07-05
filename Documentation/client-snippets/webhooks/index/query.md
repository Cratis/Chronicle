```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Webhooks;

public class WebhooksIndexQuery(IEventStore eventStore)
{
    public async Task<IEnumerable<WebhookDefinition>> GetAllWebhooks() => await eventStore.Webhooks.GetAll();
}
```
