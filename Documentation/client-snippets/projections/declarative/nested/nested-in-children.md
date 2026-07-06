```csharp title="Nested object in children"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record ProjectCreatedWithNestedChildren(string Name);

[EventType]
public record TaskAddedWithNestedChild(Guid TaskId, string Title);

[EventType]
public record TaskAssignedWithNestedChild(Guid TaskId, string Name, string Email);

[EventType]
public record TaskUnassignedWithNestedChild(Guid TaskId);

public record ProjectWithDeclarativeNestedChildren(
    string Name,
    IEnumerable<TaskWithNestedAssignee> Tasks);

public record TaskWithNestedAssignee(
    Guid TaskId,
    string Title,
    AssigneeForNestedChild? Assignee);

public record AssigneeForNestedChild(
    string Name,
    string Email);

public class ProjectProjectionWithDeclarativeNestedChildren : IProjectionFor<ProjectWithDeclarativeNestedChildren>
{
    public void Define(IProjectionBuilderFor<ProjectWithDeclarativeNestedChildren> builder) => builder
        .From<ProjectCreatedWithNestedChildren>()
        .Children(m => m.Tasks, tasks => tasks
            .IdentifiedBy(m => m.TaskId)
            .From<TaskAddedWithNestedChild>(b => b
                .UsingKey(e => e.TaskId))
            .Nested(m => m.Assignee, assignee => assignee
                .From<TaskAssignedWithNestedChild>(b => b
                    .UsingKey(e => e.TaskId))
                .ClearWith<TaskUnassignedWithNestedChild>()));
}
```
