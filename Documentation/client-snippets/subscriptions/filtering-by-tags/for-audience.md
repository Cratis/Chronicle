```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

[EventType]
[Public]
public record TagFilterWelcomeSent(string Email);

public static class SubscriptionsTagFilterForAudience
{
    public static Task Run(IEventStore eventStore) =>
        eventStore.Subscriptions.Subscribe(
            "operator",
            "operator-service",
            builder => builder
                .WithEventType<TagFilterWelcomeSent>()
                .ForAudience("lobby-a"));
}
```
