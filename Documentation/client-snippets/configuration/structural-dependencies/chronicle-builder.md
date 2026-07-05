```csharp
using Cratis.Chronicle.Identities;
using Microsoft.Extensions.Hosting;

public static class StructuralDependenciesChronicleBuilderRegistration
{
    public static void Configure(string[] args, IClientArtifactsProvider myCustomProvider, IIdentityProvider myIdentityProvider)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(
            configureOptions: options =>                         // runtime config — bindable from appsettings.json
            {
                options.EventStore = "my-store";
                options.ConnectionString = "chronicle://server:35000";
            },
            configure: b => b                                    // structural dependencies
                .WithArtifactsProvider(myCustomProvider)
                .WithIdentityProvider(myIdentityProvider));
    }
}
```
