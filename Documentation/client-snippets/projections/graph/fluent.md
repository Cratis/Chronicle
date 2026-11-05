```csharp
[Graph("Organization")]
public class OrganizationGraph : IGraphProjection
{
    public void Define(IGraphProjectionBuilder builder) => builder
        .Node<Person>(n => n
            .IdentifiedBy(p => p.PersonId)
            .From<PersonRegistered>()
            .RemovedWith<PersonRemoved>(b => b.Detach()))
        .Node<Group>(n => n
            .IdentifiedBy(g => g.GroupId)
            .From<GroupCreated>())
        .Edge<MemberOf, Person, Group>(e => e
            .From<MemberAdded>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)
                .Set(m => m.Since).ToEventContextProperty(c => c.Occurred))
            .RemovedWith<MemberRemoved>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)));
}
```
