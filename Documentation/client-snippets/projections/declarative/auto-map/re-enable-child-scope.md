```csharp title="Re-enable AutoMap for children"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record AutoMapTeamFormed(string TeamName);

[EventType]
public record AutoMapMemberJoinedTeam(string MemberId, string DisplayName);

public record AutoMapTeamMember(string MemberId, string DisplayName);

public record AutoMapTeam(
    string Name,
    DateTimeOffset CreatedAt,
    IEnumerable<AutoMapTeamMember> Members);

public class AutoMapTeamProjection : IProjectionFor<AutoMapTeam>
{
    public void Define(IProjectionBuilderFor<AutoMapTeam> builder) => builder
        .NoAutoMap()
        .From<AutoMapTeamFormed>(_ => _
            .Set(m => m.Name).To(e => e.TeamName)
            .Set(m => m.CreatedAt).ToEventContextProperty(c => c.Occurred))
        .Children(m => m.Members, children => children
            .IdentifiedBy(m => m.MemberId)
            .AutoMap()
            .From<AutoMapMemberJoinedTeam>(_ => _
                .UsingKey(e => e.MemberId)));
}
```
