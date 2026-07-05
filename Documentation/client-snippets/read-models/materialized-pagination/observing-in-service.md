```csharp
using Cratis.Chronicle;

public class MaterializedPaginationProductDashboard : IDisposable
{
    readonly IDisposable _subscription;

    public MaterializedPaginationProductDashboard(IEventStore eventStore)
    {
        _subscription = eventStore.ReadModels.Materialized
            .ObserveInstances<MaterializedPaginationProduct>(take: 100)
            .Subscribe(UpdateView);
    }

    void UpdateView(IEnumerable<MaterializedPaginationProduct> products) { /* ... */ }

    public void Dispose() => _subscription.Dispose();
}
```
