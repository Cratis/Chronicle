```csharp
var result = await eventLog.Append(eventSourceId, new OrderPlaced(customerId, total));

if (result.HasErrors)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Schema error: {error}");
    }
}
```
