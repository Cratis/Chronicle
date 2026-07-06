```csharp
using var subscription = eventStore.ReadModels
    .Watch<Order>()
    .Where(changeset => changeset.ReadModel?.TotalAmount > threshold)
    .Subscribe(changeset =>
    {
        Console.WriteLine($"{changeset.ModelKey}: {changeset.ReadModel!.TotalAmount:C}");
    });
```
