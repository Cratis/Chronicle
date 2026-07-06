```csharp
using Cratis.Chronicle;

public class MaterializedPaginationBasicUsage(IEventStore eventStore)
{
    public Task<IEnumerable<MaterializedPaginationOrder>> GetOrders() =>
        eventStore.ReadModels.Materialized.GetInstances<MaterializedPaginationOrder>();
}
```
