```csharp
var orders = await eventStore.ReadModels.GetInstances<Order>(eventCount: 1_000);

Console.WriteLine($"Replayed {orders.Count()} orders from the capped history.");
```
