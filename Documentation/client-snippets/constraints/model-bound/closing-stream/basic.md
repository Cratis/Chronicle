```csharp
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[ClosesStream(Dimensions = ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamType)]
public record ConstraintsModelBoundClosingReportDelivered(string Recipient);
```
