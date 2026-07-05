```csharp
using Cratis.Chronicle;

public static class NamespacesDotNetClientDefaultResolver
{
    public static ChronicleClient Create(ChronicleOptions options)
    {
        var resolver = new DefaultEventStoreNamespaceResolver();
        return new ChronicleClient(options, namespaceResolver: resolver);
    }
}
```
