```csharp
using Cratis.Chronicle;

public static class NamespacesDotNetClientTenantResolverUsage
{
    public static ChronicleClient Create(ChronicleOptions options, ITenantContext tenantContext) =>
        new(options, namespaceResolver: new TenantNamespaceResolver(tenantContext));
}
```
