```csharp
using Microsoft.AspNetCore.Builder;

public static class NamespacesAspNetCoreSubdomainResolver
{
    public static void Configure(WebApplicationBuilder builder) =>
        builder.AddCratisChronicle(options =>
        {
            options.EventStore = "my-event-store";
            options.WithSubdomainNamespaceResolver();
        });
}
```
