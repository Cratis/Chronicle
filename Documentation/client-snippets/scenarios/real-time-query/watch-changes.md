```csharp
using Cratis.Chronicle;

public class ScenariosQueryBookWatcher(IEventStore eventStore)
{
    public IDisposable Watch() =>
        eventStore.ReadModels.Watch<ScenariosQueryBook>()
            .Subscribe(changeset =>
            {
                if (changeset.Removed || changeset.ReadModel is null)
                {
                    return;
                }

                Console.WriteLine($"{changeset.ModelKey}: on loan = {changeset.ReadModel.OnLoan}");
            });
}
```
