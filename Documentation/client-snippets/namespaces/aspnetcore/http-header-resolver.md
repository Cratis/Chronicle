```csharp
using Microsoft.AspNetCore.Builder;

public static class NamespacesAspNetCoreHttpHeaderResolver
{
    public static void Configure(WebApplicationBuilder builder) =>
        builder.AddCratisChronicle(options =>
        {
            options.EventStore = "my-event-store";
            options.WithHttpHeaderNamespaceResolver("x-cratis-tenant-id"); // Default header name
        });
}
```
