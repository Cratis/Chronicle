```csharp
using Cratis.Chronicle;

public class ScenariosQueryLiveBookPage(IEventStore eventStore)
{
    public IDisposable Subscribe(Action<IEnumerable<ScenariosQueryBook>> updateView) =>
        eventStore.ReadModels.Materialized
            .ObserveInstances<ScenariosQueryBook>(take: 50)
            .Subscribe(books => updateView(books));
}
```
