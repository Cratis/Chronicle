```csharp
public class DecRemoveWithJoinGroupMembershipProjection : IProjectionFor<DecRemoveWithJoinUserProfile>
{
    public void Define(IProjectionBuilderFor<DecRemoveWithJoinUserProfile> builder) => builder
        .AutoMap()
        .From<DecRemoveWithJoinUserRegistered>(_ => _
            .Set(m => m.UserId).ToEventSourceId()
            .Set(m => m.RegisteredAt).ToEventContextProperty(c => c.Occurred))
        .Children(m => m.Memberships, children => children
            .IdentifiedBy(e => e.GroupId)
            .AutoMap()
            .From<DecRemoveWithJoinUserJoinedGroup>(_ => _
                .UsingParentKey(e => e.UserId)
                .UsingKey(e => e.GroupId)
                .Set(m => m.JoinedAt).ToEventContextProperty(c => c.Occurred))
            .Join<DecRemoveWithJoinGroupCreated>(_ => _
                .On(m => m.GroupId))
            .RemovedWith<DecRemoveWithJoinUserLeftGroup>(_ => _
                .UsingParentKey(e => e.UserId)
                .UsingKey(e => e.GroupId))
            .RemovedWithJoin<DecRemoveWithJoinGroupDisbanded>());
}
```
