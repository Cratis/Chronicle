```csharp
using Cratis.Chronicle;

public record MaterializedPaginationOrder(string CustomerName, decimal Total);

public class MaterializedPaginationAccessingApi(IEventStore eventStore)
{
    public async Task<IEnumerable<MaterializedPaginationOrder>> GetOrders()
    {
        // Inject IEventStore, then reach through to the Materialized API
        var instances = await eventStore.ReadModels.Materialized.GetInstances<MaterializedPaginationOrder>();
        return instances;
    }
}
```
