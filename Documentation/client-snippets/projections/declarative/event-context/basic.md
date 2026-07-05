```csharp
using Cratis.Chronicle.Projections;

public class DecEventContextUserActivityProjection : IProjectionFor<DecEventContextUserActivity>
{
    public void Define(IProjectionBuilderFor<DecEventContextUserActivity> builder) => builder
        .From<DecEventContextUserLoggedIn>(_ => _
            .Set(m => m.UserId).ToEventSourceId()
            .Set(m => m.LastLogin).ToEventContextProperty(c => c.Occurred))
        .From<DecEventContextUserPerformedAction>(_ => _
            .Set(m => m.UserId).ToEventSourceId()
            .Set(m => m.LastActivity).ToEventContextProperty(c => c.Occurred));
}
```
