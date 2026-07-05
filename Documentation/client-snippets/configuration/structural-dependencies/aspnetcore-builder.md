```csharp
using Cratis.Chronicle.Identities;
using Microsoft.AspNetCore.Builder;

public static class StructuralDependenciesAspNetCoreBuilderRegistration
{
    public static void Configure(string[] args, IIdentityProvider myIdentityProvider)
    {
        // ASP.NET Core — WebApplicationBuilder
        var builder = WebApplication.CreateBuilder(args);
        builder.AddCratisChronicle(
            configureOptions: options => options.EventStore = "my-store",
            configure: b => b
                .WithIdentityProvider(myIdentityProvider));
    }
}
```
