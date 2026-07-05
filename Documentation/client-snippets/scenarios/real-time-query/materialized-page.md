```csharp
using Cratis.Chronicle;

public class ScenariosQueryBookPageService(IEventStore eventStore)
{
    public Task<IEnumerable<ScenariosQueryBook>> GetPage() =>
        eventStore.ReadModels.Materialized.GetInstances<ScenariosQueryBook>(skip: 0, take: 20);
}
```
