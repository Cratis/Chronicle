```csharp
using Cratis.Chronicle.Projections;

public class DecJoinsUserProjection : IProjectionFor<DecJoinsUser>
{
    public void Define(IProjectionBuilderFor<DecJoinsUser> builder) => builder
        .AutoMap()
        .From<DecJoinsUserCreated>()
        .From<DecJoinsUserAssignedToGroup>(b => b
            .UsingKey(e => e.UserId)
            .Set(m => m.GroupId).ToEventSourceId())
        .Join<DecJoinsGroupCreated>(j => j
            .On(m => m.GroupId))
        .Join<DecJoinsGroupRenamed>(j => j
            .On(m => m.GroupId));
}
```
