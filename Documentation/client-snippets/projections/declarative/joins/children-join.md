```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecJoinsChildTaskAssigned(string TaskId, string ProjectId);

[EventType]
public record DecJoinsChildProjectCreated(string Name);

public record DecJoinsChildTask(string TaskId, string ProjectId, string? ProjectName);

public record DecJoinsChildProjectBoard(IEnumerable<DecJoinsChildTask> Tasks);

public class DecJoinsChildProjectBoardProjection : IProjectionFor<DecJoinsChildProjectBoard>
{
    public void Define(IProjectionBuilderFor<DecJoinsChildProjectBoard> builder) => builder
        .Children(m => m.Tasks, children => children
            .IdentifiedBy(e => e.TaskId)
            .AutoMap()
            .From<DecJoinsChildTaskAssigned>(b => b
                .UsingKey(e => e.TaskId))
            .Join<DecJoinsChildProjectCreated>(j => j
                .On(m => m.ProjectId)));
}
```
