```csharp
using Cratis.Chronicle;

public record DesigningReadModelsCustomerListItem(Guid Id, string Name);

public class DesigningReadModelsCustomerListService(IEventStore eventStore)
{
    public async Task<IEnumerable<DesigningReadModelsCustomerListItem>> GetAllStronglyConsistent()
    {
        // Strongly consistent — Chronicle replays the read model's events on demand
        return await eventStore.ReadModels.GetInstances<DesigningReadModelsCustomerListItem>();
    }

    public async Task<IEnumerable<DesigningReadModelsCustomerListItem>> GetPageEventuallyConsistent()
    {
        // Eventually consistent — a page of materialized instances straight from storage
        return await eventStore.ReadModels.Materialized.GetInstances<DesigningReadModelsCustomerListItem>(skip: 0, take: 20);
    }
}
```
