```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;

[EventType]
public record ConfiguredSourceUserInvited(Guid UserId);

public class ConfiguredSourceUserInvitedReactor : IReactor
{
    public Task Invited(ConfiguredSourceUserInvited @event) => Task.CompletedTask;
}

public static class ConfiguredSourceRegistration
{
    public static void Register(ChronicleOptions options, string hostEventStore) =>
        options.ExplicitArtifacts.RegisterReactor<ConfiguredSourceUserInvitedReactor>(
            ReactorRegistrationOptions.FromEventStore(hostEventStore));
}
```
