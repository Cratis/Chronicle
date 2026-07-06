```csharp
[EventType]
public record MbConstantKeyUserRegistered;

[EventType]
public record MbConstantKeyOrderPlacedGlobal;

[FromEvent<MbConstantKeyUserRegistered>]
public record MbConstantKeyUserDashboard(
    [Key]
    Guid UserId,

    string Name,

    // A per-instance property alongside a constant-keyed one on the same read model
    [Count<MbConstantKeyOrderPlacedGlobal>(ConstantKey = "global-stats")]
    int PlatformTotalOrders);
```
