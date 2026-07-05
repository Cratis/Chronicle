```csharp
using Cratis.Types;

public static class StructuralDepsDefaultArtifactsProvider
{
    public static DefaultClientArtifactsProvider Create()
    {
        var assembliesProvider = new CompositeAssemblyProvider(
            ProjectReferencedAssemblies.Instance,
            PackageReferencedAssemblies.Instance);

        return new DefaultClientArtifactsProvider(assembliesProvider);
    }
}
```
