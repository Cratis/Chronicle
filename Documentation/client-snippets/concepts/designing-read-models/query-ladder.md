```csharp
using Cratis.Chronicle;

public record DesigningReadModelsCustomerListItem(Guid Id, string Name);

public class DesigningReadModelsCustomerListService(IEventStore eventStore)
{
    public async Task<IEnumerable<DesigningReadModelsCustomerListItem>> GetEveryInstance()
    {
        // Every instance in one call — read from the materialized store
        return await eventStore.ReadModels.GetInstances<DesigningReadModelsCustomerListItem>();
    }

    public async Task<IEnumerable<DesigningReadModelsCustomerListItem>> GetPage()
    {
        // One page of materialized instances, with paging done by the store
        return await eventStore.ReadModels.Materialized.GetInstances<DesigningReadModelsCustomerListItem>(skip: 0, take: 20);
    }
}
```
