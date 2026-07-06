```csharp
using Cratis.Chronicle;

public class NamespacesDotNetClientSampleResolver : IEventStoreNamespaceResolver
{
    public EventStoreNamespaceName Resolve() => EventStoreNamespaceName.Default;
}
```
