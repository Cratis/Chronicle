```csharp
using Microsoft.Extensions.Hosting;

public static class NamespacesDotNetClientHostedAppConfiguration
{
    public static void ConfigureViaOptions(IHostApplicationBuilder builder) =>
        // Via options (type is resolved from DI)
        builder.AddCratisChronicle(options =>
        {
            options.EventStore = "my-store";
            options.EventStoreNamespaceResolverType = typeof(TenantNamespaceResolver);
        });

    public static void ConfigureViaBuilder(IHostApplicationBuilder builder, ITenantContext tenantContext) =>
        // Via builder (instance is used directly)
        builder.AddCratisChronicle(
            configureOptions: options => options.EventStore = "my-store",
            configure: b => b.WithNamespaceResolver(new TenantNamespaceResolver(tenantContext)));
}
```
