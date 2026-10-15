```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[ClosesStream(Name = "close-month", EventStreamIdFrom = nameof(Month), ReopenedBy = [typeof(ConstraintsModelBoundClosingMonthReopened)])]
public record ConstraintsModelBoundClosingMonthClosed(string Month);

[EventType]
public record ConstraintsModelBoundClosingMonthReopened(string Month, string Reason);
```
