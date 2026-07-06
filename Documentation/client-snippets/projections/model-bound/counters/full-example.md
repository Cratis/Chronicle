```csharp
// Events
[EventType]
public record MbCountersUserLoggedInFull(DateTimeOffset Timestamp);

[EventType]
public record MbCountersUserLoggedOutFull(DateTimeOffset Timestamp);

[EventType]
public record MbCountersPurchaseMade(decimal Amount);

[EventType]
public record MbCountersRefundIssued(decimal Amount);

// Read Model
public record MbCountersUserActivity(
    [Key]
    Guid UserId,

    // Track login/logout counts
    [Count<MbCountersUserLoggedInFull>]
    int TotalLogins,

    [Count<MbCountersUserLoggedOutFull>]
    int TotalLogouts,

    // Track active sessions
    [Increment<MbCountersUserLoggedInFull>]
    [Decrement<MbCountersUserLoggedOutFull>]
    int ActiveSessions,

    // Track transaction counts
    [Count<MbCountersPurchaseMade>]
    int PurchaseCount,

    [Count<MbCountersRefundIssued>]
    int RefundCount,

    // Track transaction values
    [AddFrom<MbCountersPurchaseMade>(nameof(MbCountersPurchaseMade.Amount))]
    [SubtractFrom<MbCountersRefundIssued>(nameof(MbCountersRefundIssued.Amount))]
    decimal NetSpent);
```
