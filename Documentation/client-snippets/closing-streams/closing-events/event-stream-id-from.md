```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[ClosesStream(EventStreamIdFrom = nameof(Period), ReopenedBy = [typeof(ClosingEventsBooksReopened)])]
public record ClosingEventsBooksClosed(string Period, decimal ClosingBalance);

[EventType]
public record ClosingEventsBooksReopened(string Period, string Reason);
```
