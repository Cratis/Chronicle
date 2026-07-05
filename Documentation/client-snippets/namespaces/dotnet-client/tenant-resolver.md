```csharp
using Cratis.Chronicle;

public interface ITenantContext
{
    EventStoreNamespaceName CurrentTenantId { get; }
}

public class TenantNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly ITenantContext _tenantContext;

    public TenantNamespaceResolver(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public EventStoreNamespaceName Resolve() =>
        _tenantContext.CurrentTenantId;
}
```
