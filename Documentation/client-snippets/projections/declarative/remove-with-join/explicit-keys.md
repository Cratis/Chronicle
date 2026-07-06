```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecRemoveWithJoinExplicitEmployeeHired(string Name);

[EventType]
public record DecRemoveWithJoinExplicitEmployeeAssignedToProject(string EmployeeId, string ProjectId);

[EventType]
public record DecRemoveWithJoinExplicitProjectCreated(string Name);

[EventType]
public record DecRemoveWithJoinExplicitProjectCancelled(string ProjectId);

public record DecRemoveWithJoinExplicitEmployeeProject(string ProjectId, string Name, DateTimeOffset AssignedAt);

public record DecRemoveWithJoinExplicitEmployee(string Name, IEnumerable<DecRemoveWithJoinExplicitEmployeeProject> Projects);

public class DecRemoveWithJoinExplicitEmployeeProjection : IProjectionFor<DecRemoveWithJoinExplicitEmployee>
{
    public void Define(IProjectionBuilderFor<DecRemoveWithJoinExplicitEmployee> builder) => builder
        .AutoMap()
        .From<DecRemoveWithJoinExplicitEmployeeHired>()
        .Children(m => m.Projects, children => children
            .IdentifiedBy(e => e.ProjectId)
            .AutoMap()
            .From<DecRemoveWithJoinExplicitEmployeeAssignedToProject>(_ => _
                .UsingParentKey(e => e.EmployeeId)
                .UsingKey(e => e.ProjectId)
                .Set(m => m.AssignedAt).ToEventContextProperty(c => c.Occurred))
            .Join<DecRemoveWithJoinExplicitProjectCreated>(_ => _
                .On(m => m.ProjectId))
            .RemovedWithJoin<DecRemoveWithJoinExplicitProjectCancelled>(_ => _
                .UsingKey(e => e.ProjectId)));
}
```
