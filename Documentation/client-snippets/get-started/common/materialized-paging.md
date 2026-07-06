```csharp
using Cratis.Chronicle;

public class GetStartedBookPagingService(IEventStore eventStore)
{
    public async Task<IEnumerable<GetStartedBook>> GetPage() =>
        await eventStore.ReadModels.Materialized.GetInstances<GetStartedBook>(skip: 0, take: 20);
}
```
