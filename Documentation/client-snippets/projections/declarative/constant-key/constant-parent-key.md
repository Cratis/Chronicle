```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecConstantKeyUserJoined(string UserId, string UserName);

public record DecConstantKeyTeamMember(string UserId, string Name);

public record DecConstantKeyTeam(IEnumerable<DecConstantKeyTeamMember> Members);

public class DecConstantKeyTeamActivityProjection : IProjectionFor<DecConstantKeyTeam>
{
    public void Define(IProjectionBuilderFor<DecConstantKeyTeam> builder) => builder
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.UserId)
            .From<DecConstantKeyUserJoined>(_ => _
                .UsingConstantParentKey("main-team")
                .Set(m => m.Name).To(e => e.UserName)));
}
```
