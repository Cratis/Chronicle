```csharp
public class ProjectionsManager : Grain, IProjectionsManager
{
    public Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant) =>
        RegisterWithVersion(definitions, fullSetOwner, registrant, version: null);

    public Task RegisterWithVersion(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version) =>
        Task.CompletedTask;
}
```
