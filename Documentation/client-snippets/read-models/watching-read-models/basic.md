```csharp
using var subscription = eventStore.ReadModels
    .Watch<Order>()
    .Subscribe(changeset =>
    {
        if (changeset.Removed || changeset.ReadModel is null)
        {
            return;
        }

        Console.WriteLine($"{changeset.ModelKey}: {changeset.ReadModel.Status}");
    });
```
