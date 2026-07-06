```csharp
[EventType]
public record MbCountersOrderPlaced;

[EventType]
public record MbCountersOrderCancelled;

public record MbCountersEventMetrics(
    [Key]
    Guid Id,

    [Count<MbCountersOrderPlaced>]
    int TotalOrders,

    [Count<MbCountersOrderCancelled>]
    int CancelledOrders);
```
