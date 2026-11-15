```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
[Public]
public record OperatorInvitationIssued([property: Audience] string Lobby, string Email);

[EventType]
[Public]
public record OperatorNoticePublished(string Message);

public static class OperatorAddressesTwoLobbies
{
    // The operator side. The audience comes from the [Audience] property.
    public static async Task Invite(IEventStore operatorStore)
    {
        var outbox = operatorStore.GetEventSequence(EventSequenceId.Outbox);
        await outbox.Append("invitation-1", new OperatorInvitationIssued("lobby-a", "ada@example.com"));
        await outbox.Append("invitation-2", new OperatorInvitationIssued("lobby-b", "grace@example.com"));
    }

    // One notice, two audiences: nested scopes add up.
    public static async Task Announce(IEventStore operatorStore)
    {
        using var lobbyA = EventTags.Audience("lobby-a");
        using var lobbyB = EventTags.Audience("lobby-b");
        await operatorStore.GetEventSequence(EventSequenceId.Outbox)
            .Append("notice-1", new OperatorNoticePublished("Maintenance tonight"));
    }

    // Each lobby subscribes to the operator's outbox for itself only.
    public static Task Subscribe(IEventStore lobbyStore, string lobby) =>
        lobbyStore.Subscriptions.Subscribe(
            "operator",
            "operator-service",
            builder => builder
                .WithEventType<OperatorInvitationIssued>()
                .WithEventType<OperatorNoticePublished>()
                .ForAudience(lobby));
}
```
