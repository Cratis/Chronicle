```csharp
using Cratis.Chronicle.EventSequences;

public static class SubscriptionsOutboxInboxId
{
    public static EventSequenceId Resolve()
    {
        var inboxId = new EventSequenceId($"{EventSequenceId.InboxPrefix}source-event-store");
        // Resolves to: "inbox-source-event-store"
        return inboxId;
    }
}
```
