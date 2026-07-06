```csharp title="Explicit parent key from context"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record GroupCreatedWithContextParentKey(string Name);

[EventType]
public record UserAddedWithContextParentKey(string UserId, string Role);

public record GroupWithContextParentKey(
    string Name,
    IEnumerable<GroupMemberWithContextParentKey> Members);

public record GroupMemberWithContextParentKey(
    string UserId,
    string Role);

public class GroupWithContextParentKeyProjection : IProjectionFor<GroupWithContextParentKey>
{
    public void Define(IProjectionBuilderFor<GroupWithContextParentKey> builder) => builder
        .From<GroupCreatedWithContextParentKey>()
        .Children(m => m.Members, children => children
            .IdentifiedBy(m => m.UserId)
            .From<UserAddedWithContextParentKey>(b => b
                .UsingParentKeyFromContext(c => c.EventSourceId)
                .UsingKey(e => e.UserId)));
}
```
