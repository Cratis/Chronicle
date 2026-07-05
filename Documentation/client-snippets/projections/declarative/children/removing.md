```csharp title="Remove children"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record GroupCreatedWithRemoval(string Name);

[EventType]
public record UserAddedWithRemoval(string UserId, string Role);

[EventType]
public record UserRemovedWithRemoval(string UserId);

public record GroupWithRemoval(
    string Name,
    IEnumerable<GroupMemberWithRemoval> Members);

public record GroupMemberWithRemoval(
    string UserId,
    string Role);

public class GroupWithRemovalProjection : IProjectionFor<GroupWithRemoval>
{
    public void Define(IProjectionBuilderFor<GroupWithRemoval> builder) => builder
        .From<GroupCreatedWithRemoval>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(m => m.UserId)
            .From<UserAddedWithRemoval>(b => b
                .UsingKey(e => e.UserId))
            .RemovedWith<UserRemovedWithRemoval>(b => b
                .UsingKey(e => e.UserId)));
}
```
