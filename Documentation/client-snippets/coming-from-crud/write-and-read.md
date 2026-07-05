```csharp
using Cratis.Chronicle.Events;

public class CrudComparisonCustomerAddressUpdater(IEventStore eventStore)
{
    public async Task<CrudComparisonCustomerCard> ChangeAddress(EventSourceId customerId, string newAddress)
    {
        await eventStore.EventLog.Append(customerId, new CrudComparisonAddressChanged(newAddress));
        return await eventStore.ReadModels.GetInstanceById<CrudComparisonCustomerCard>(customerId);
    }
}
```
