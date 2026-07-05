```csharp
[EventType]
public record MbCountersUserConnected;

[EventType]
public record MbCountersUserDisconnected;

public record MbCountersServerStatistics(
    [Key]
    Guid ServerId,

    [Increment<MbCountersUserConnected>]
    [Decrement<MbCountersUserDisconnected>]
    int ActiveConnections);
```
