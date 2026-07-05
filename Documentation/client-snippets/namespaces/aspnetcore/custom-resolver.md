```csharp
using Cratis.Chronicle;
using Microsoft.Extensions.Configuration;

public class CustomNamespaceResolver : IEventStoreNamespaceResolver
{
    readonly IConfiguration _configuration;

    public CustomNamespaceResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public EventStoreNamespaceName Resolve() =>
        _configuration["Tenant:Namespace"] ?? EventStoreNamespaceName.Default;
}
```
