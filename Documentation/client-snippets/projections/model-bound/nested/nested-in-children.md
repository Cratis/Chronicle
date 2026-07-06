```csharp title="Nested object inside child collection items"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record TaskAddedForNestedChildren(Guid TaskId, string Title);

[EventType]
public record TaskAssignedForNestedChildren(Guid TaskId, string Name, string Email);

[EventType]
public record TaskUnassignedForNestedChildren(Guid TaskId);

public record ProjectWithNestedChildren(
    [Key] Guid Id,
    string Name,
    [ChildrenFrom<TaskAddedForNestedChildren>(key: nameof(TaskAddedForNestedChildren.TaskId))]
    IEnumerable<ProjectTaskWithNestedAssignee> Tasks);

public record ProjectTaskWithNestedAssignee(
    [Key] Guid TaskId,
    string Title,
    [Nested] TaskAssigneeNestedChild? Assignee);

[FromEvent<TaskAssignedForNestedChildren>]
[ClearWith<TaskUnassignedForNestedChildren>]
public record TaskAssigneeNestedChild(
    string Name,
    string Email);
```
