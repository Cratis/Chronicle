```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;

public class MaterializedPaginationNamedConstants(IEventStore eventStore)
{
    public Task<IEnumerable<MaterializedPaginationOrder>> GetOrders() =>
        // Using named constants
        eventStore.ReadModels.Materialized.GetInstances<MaterializedPaginationOrder>(
            skip: InstanceCountToSkip.Zero,
            take: InstanceCount.Default);
}
```
