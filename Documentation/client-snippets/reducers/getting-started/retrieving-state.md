```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

public class ReducersGettingStartedOrderService(IEventStore eventStore)
{
    public async Task<ReducersGettingStartedOrderSummary?> GetOrderSummary(Guid orderId) =>
        await eventStore.ReadModels.GetInstanceById<ReducersGettingStartedOrderSummary>(orderId);
}
```
