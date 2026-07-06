```csharp title="Parent key from event content"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record GroupCreatedWithEventParentKey(string Name);

[EventType]
public record UserAddedWithEventParentKey(string GroupId, string UserId, string Role);

public record GroupWithEventParentKey(
    string Name,
    IEnumerable<GroupMemberWithEventParentKey> Members);

public record GroupMemberWithEventParentKey(
    string UserId,
    string Role);

public class GroupWithEventParentKeyProjection : IProjectionFor<GroupWithEventParentKey>
{
    public void Define(IProjectionBuilderFor<GroupWithEventParentKey> builder) => builder
        .From<GroupCreatedWithEventParentKey>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(m => m.UserId)
            .From<UserAddedWithEventParentKey>(b => b
                .UsingParentKey(e => e.GroupId)
                .UsingKey(e => e.UserId)));
}
```
