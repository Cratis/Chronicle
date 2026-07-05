```csharp
var snapshots = await eventStore.ReadModels.GetSnapshotsById<Order>(orderId);

foreach (var snapshot in snapshots)
{
    Console.WriteLine($"Snapshot at {snapshot.Occurred}:");
    Console.WriteLine($"  Correlation ID: {snapshot.CorrelationId}");
    Console.WriteLine($"  Event count: {snapshot.Events.Count()}");
    Console.WriteLine($"  State: {snapshot.Instance}");
}
```
