```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

[EventType]
public record ProductListed(string Name);

public interface IProductSearch
{
    Task Index(IEnumerable<string> names, CancellationToken cancellationToken);
}

[BatchSize(MaxEvents = 1000, MaxBytes = 4_194_304, MaxDelayMilliseconds = 50)]
public class ProductSearchIndexer(IProductSearch search) : IReactor
{
    [Observes<ProductListed>]
    public Task Index(EventBatch batch, BatchContext context) =>
        search.Index(batch.Select(e => ((ProductListed)e.Event).Name), context.CancellationToken);
}
```
