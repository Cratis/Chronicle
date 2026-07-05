```csharp
using System.Linq;
using Cratis.Chronicle;

public record MaterializedPaginationProduct(string Name, decimal Price);

public class MaterializedPaginationObserving(IEventStore eventStore)
{
    public void Run()
    {
        var subscription = eventStore.ReadModels.Materialized
            .ObserveInstances<MaterializedPaginationProduct>(take: 50)
            .Subscribe(products =>
            {
                // Called whenever the stored instances change
                Console.WriteLine($"Products updated: {products.Count()} in view");
            });

        // Dispose when done to release the change stream
        subscription.Dispose();
    }
}
```
