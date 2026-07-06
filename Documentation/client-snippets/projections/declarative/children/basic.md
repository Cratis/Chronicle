```csharp title="Projection with children"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record GroupCreatedForChildren(string Name, string Description);

[EventType]
public record UserAddedToGroupForChildren(string UserId, string Role);

[EventType]
public record UserRoleChangedForChildren(string UserId, string Role);

[EventType]
public record UserRemovedFromGroupForChildren(string UserId);

public record GroupForChildren(
    string Name,
    string Description,
    IEnumerable<GroupMemberForChildren> Members);

public record GroupMemberForChildren(
    string UserId,
    string Role);

public class GroupProjectionForChildren : IProjectionFor<GroupForChildren>
{
    public void Define(IProjectionBuilderFor<GroupForChildren> builder) => builder
        .From<GroupCreatedForChildren>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(m => m.UserId)
            .From<UserAddedToGroupForChildren>(b => b
                .UsingKey(e => e.UserId))
            .From<UserRoleChangedForChildren>(b => b
                .UsingKey(e => e.UserId))
            .RemovedWith<UserRemovedFromGroupForChildren>(b => b
                .UsingKey(e => e.UserId)));
}
```
