```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Specifications;
using Xunit;

[EventType("pure-function-vibe-cancelled")]
public record TestingPureVibeCancelled();

public record TestingPureCreateNotification(string Host);

public record TestingPureVibeAttendees(string Host);

// The reactor returns the command as a side effect, so its logic is a pure
// function of (event, read model) — no IEventLog or ICommandPipeline injected.
public class TestingPureCancellationReactor : IReactor
{
    public Task<TestingPureCreateNotification> VibeCancelled(
        TestingPureVibeCancelled @event,
        TestingPureVibeAttendees attendees) =>
        Task.FromResult(new TestingPureCreateNotification(attendees.Host));
}

public class when_a_vibe_is_cancelled : Specification
{
    readonly TestingPureCancellationReactor _reactor = new();
    readonly TestingPureVibeAttendees _attendees = new("Ada");
    TestingPureCreateNotification _command = default!;

    async Task Because() => _command = await _reactor.VibeCancelled(new TestingPureVibeCancelled(), _attendees);

    [Fact] void should_request_a_notification_for_the_host() => _command.Host.ShouldEqual("Ada");
}
```
