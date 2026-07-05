```csharp
[EventType]
public record MbSetValueSubscriptionStarted;

[EventType]
public record MbSetValueSubscriptionPaused;

[EventType]
public record MbSetValueSubscriptionCanceled;

public record MbSetValueSubscription(
    [Key]
    Guid Id,

    [SetValue<MbSetValueSubscriptionStarted>("active")]
    [SetValue<MbSetValueSubscriptionPaused>("paused")]
    [SetValue<MbSetValueSubscriptionCanceled>("canceled")]
    string State);
```
