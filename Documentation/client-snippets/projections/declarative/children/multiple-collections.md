```csharp title="Multiple child collections"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record GroupCreatedWithMultipleCollections(string Name);

[EventType]
public record MemberAddedToGroup(string UserId, string Role);

[EventType]
public record TaskAssignedToGroup(string TaskId, string Title);

public record GroupWithMultipleCollections(
    string Name,
    IEnumerable<GroupMemberInMultipleCollections> Members,
    IEnumerable<GroupTaskInMultipleCollections> Tasks);

public record GroupMemberInMultipleCollections(
    string UserId,
    string Role);

public record GroupTaskInMultipleCollections(
    string TaskId,
    string Title);

public class GroupWithMultipleCollectionsProjection : IProjectionFor<GroupWithMultipleCollections>
{
    public void Define(IProjectionBuilderFor<GroupWithMultipleCollections> builder) => builder
        .From<GroupCreatedWithMultipleCollections>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(m => m.UserId)
            .From<MemberAddedToGroup>(b => b
                .UsingKey(e => e.UserId)))
        .Children(m => m.Tasks, children => children
            .IdentifiedBy(m => m.TaskId)
            .From<TaskAssignedToGroup>(b => b
                .UsingKey(e => e.TaskId)));
}
```
