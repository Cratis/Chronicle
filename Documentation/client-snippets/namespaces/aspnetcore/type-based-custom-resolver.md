```csharp
using Cratis.Chronicle.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class NamespacesAspNetCoreTypeBasedCustomResolver
{
    public static void Configure(WebApplicationBuilder builder) =>
        builder.Services.Configure<ChronicleAspNetCoreOptions>(options =>
        {
            options.EventStore = "my-event-store";
            options.EventStoreNamespaceResolverType = typeof(CustomNamespaceResolver);
        });
}
```
