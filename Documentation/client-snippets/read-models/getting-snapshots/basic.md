```csharp
var snapshots = await eventStore.ReadModels.GetSnapshotsById<Order>(orderId);

Console.WriteLine($"Found {snapshots.Count()} snapshots.");
```
