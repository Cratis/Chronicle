```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

public static class NamespacesAspNetCoreBuilderCustomResolver
{
    public static void Configure(WebApplicationBuilder builder, IConfiguration someConfiguration) =>
        builder.AddCratisChronicle(
            configureOptions: options => options.EventStore = "my-event-store",
            configure: b => b.WithNamespaceResolver(new CustomNamespaceResolver(someConfiguration)));
}
```
