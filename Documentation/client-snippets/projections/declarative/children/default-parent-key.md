```csharp title="Default parent key"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record GroupCreatedWithDefaultParentKey(string Name);

[EventType]
public record UserAddedWithDefaultParentKey(string UserId, string Role);

public record GroupWithDefaultParentKey(
    string Name,
    IEnumerable<GroupMemberWithDefaultParentKey> Members);

public record GroupMemberWithDefaultParentKey(
    string UserId,
    string Role);

public class GroupWithDefaultParentKeyProjection : IProjectionFor<GroupWithDefaultParentKey>
{
    public void Define(IProjectionBuilderFor<GroupWithDefaultParentKey> builder) => builder
        .From<GroupCreatedWithDefaultParentKey>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(m => m.UserId)
            .From<UserAddedWithDefaultParentKey>(b => b
                .UsingKey(e => e.UserId)));
}
```
