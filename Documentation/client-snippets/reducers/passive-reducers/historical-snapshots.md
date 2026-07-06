```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

public record PassiveReducersAccountBalance(decimal Balance);

public class PassiveReducersHistoricalBalanceService(IEventStore eventStore)
{
    public async Task<PassiveReducersAccountBalance> GetBalanceAtDate(Guid accountId, DateTimeOffset date)
    {
        // Passive reducer computes state on-demand from historical events
        return await eventStore.ReadModels.GetInstanceById<PassiveReducersAccountBalance>(accountId);
    }
}
```
