```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecRemoveWithJoinBasicUserCreated(string Name);

[EventType]
public record DecRemoveWithJoinBasicUserAddedToGroup(string UserId, string GroupId);

[EventType]
public record DecRemoveWithJoinBasicGroupCreated(string Name);

[EventType]
public record DecRemoveWithJoinBasicGroupDeleted;

public record DecRemoveWithJoinBasicUserGroup(string GroupId, string Name, DateTimeOffset JoinedAt);

public record DecRemoveWithJoinBasicUser(string Name, IEnumerable<DecRemoveWithJoinBasicUserGroup> Groups);

public class DecRemoveWithJoinBasicUserProjection : IProjectionFor<DecRemoveWithJoinBasicUser>
{
    public void Define(IProjectionBuilderFor<DecRemoveWithJoinBasicUser> builder) => builder
        .AutoMap()
        .From<DecRemoveWithJoinBasicUserCreated>()
        .Children(m => m.Groups, children => children
            .IdentifiedBy(e => e.GroupId)
            .AutoMap()
            .From<DecRemoveWithJoinBasicUserAddedToGroup>(_ => _
                .UsingParentKey(e => e.UserId)
                .Set(m => m.JoinedAt).ToEventContextProperty(c => c.Occurred))
            .Join<DecRemoveWithJoinBasicGroupCreated>(_ => _
                .On(m => m.GroupId))
            .RemovedWithJoin<DecRemoveWithJoinBasicGroupDeleted>());
}
```
