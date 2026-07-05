```csharp
using Microsoft.Extensions.Hosting;

public static class StructuralDepsCustomArtifactsProviderUsageRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(
            configureOptions: options => options.EventStore = "my-store",
            configure: b => b.WithArtifactsProvider(new StructuralDepsMyArtifactsProvider()));
    }
}
```
