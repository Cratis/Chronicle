```csharp
var result = await eventLog.Append(
    eventSourceId,
    new OrderPlaced(customerId, total),
    occurred: DateTimeOffset.Parse("2024-01-15T10:30:00Z")
);
```
