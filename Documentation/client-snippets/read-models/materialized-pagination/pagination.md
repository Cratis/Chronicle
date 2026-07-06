```csharp
using System.Linq;
using Cratis.Chronicle;

public class MaterializedPaginationPagination(IEventStore eventStore)
{
    public async Task GetPages()
    {
        // First page of 20
        var page1 = await eventStore.ReadModels.Materialized.GetInstances<MaterializedPaginationOrder>(take: 20);
        Console.WriteLine($"Page 1: {page1.Count()} orders");

        // Second page of 20
        var page2 = await eventStore.ReadModels.Materialized.GetInstances<MaterializedPaginationOrder>(skip: 20, take: 20);
        Console.WriteLine($"Page 2: {page2.Count()} orders");

        // Third page of 20
        var page3 = await eventStore.ReadModels.Materialized.GetInstances<MaterializedPaginationOrder>(skip: 40, take: 20);
        Console.WriteLine($"Page 3: {page3.Count()} orders");
    }
}
```
