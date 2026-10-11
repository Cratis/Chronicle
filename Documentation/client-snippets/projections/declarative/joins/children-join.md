```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecJoinsChildMemberAssignedToProject(string ProjectId, string MemberId);

[EventType]
public record DecJoinsChildMemberRegistered(string Name);

public record DecJoinsChildProjectMember(string MemberId, string? Name);

public record DecJoinsChildProjectBoard(IEnumerable<DecJoinsChildProjectMember> Members);

public class DecJoinsChildProjectBoardProjection : IProjectionFor<DecJoinsChildProjectBoard>
{
    public void Define(IProjectionBuilderFor<DecJoinsChildProjectBoard> builder) => builder
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.MemberId)
            .From<DecJoinsChildMemberAssignedToProject>(b => b
                .UsingParentKey(e => e.ProjectId)
                .UsingKey(e => e.MemberId))
            .Join<DecJoinsChildMemberRegistered>());
}
```
