```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[ClosesStream(EventStreamIdFrom = nameof(Period))]
public record ConstraintsModelBoundClosingBooksClosed(string Period, decimal ClosingBalance);
```
