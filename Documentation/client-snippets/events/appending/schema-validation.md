```csharp
using Cratis.Chronicle.Events.Constraints;

var result = await eventLog.Append(eventSourceId, new OrderPlaced(customerId, total));

foreach (var violation in result.ConstraintViolations.Where(v => v.ConstraintType == ConstraintType.Schema))
{
    Console.WriteLine($"Schema violation at {violation.Details["path"]}: {violation.Message}");
}
```
