```csharp
public class DecRemoveWithJoinDeveloperProjectsProjection : IProjectionFor<DecRemoveWithJoinDeveloperProfile>
{
    public void Define(IProjectionBuilderFor<DecRemoveWithJoinDeveloperProfile> builder) => builder
        .AutoMap()
        .From<DecRemoveWithJoinDeveloperOnboarded>(_ => _
            .Set(m => m.DeveloperId).ToEventSourceId()
            .Set(m => m.OnboardedAt).ToEventContextProperty(c => c.Occurred))
        .Children(m => m.CurrentProjects, children => children
            .IdentifiedBy(e => e.ProjectId)
            .AutoMap()
            .From<DecRemoveWithJoinDeveloperAssignedToProject>(_ => _
                .UsingParentKey(e => e.DeveloperId)
                .UsingKey(e => e.ProjectId)
                .Set(m => m.AssignedAt).ToEventContextProperty(c => c.Occurred))
            .Join<DecRemoveWithJoinProjectInitiated>(_ => _
                .On(m => m.ProjectId))
            .RemovedWith<DecRemoveWithJoinDeveloperUnassignedFromProject>(_ => _
                .UsingParentKey(e => e.DeveloperId)
                .UsingKey(e => e.ProjectId))
            .RemovedWithJoin<DecRemoveWithJoinProjectCancelled>()
            .RemovedWithJoin<DecRemoveWithJoinProjectCompleted>());
}
```
