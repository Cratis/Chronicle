```csharp
using Orleans.CodeGeneration;

[Version(1)]
public interface IProjectionsManager : IGrainWithStringKey
{
    Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner = null, string? registrant = null);

    Task RegisterWithVersion(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version);
}
```
