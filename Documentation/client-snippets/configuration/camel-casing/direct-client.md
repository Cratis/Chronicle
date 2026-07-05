```csharp
using Cratis.Chronicle;
using Cratis.Serialization;

public static class CamelCasingDirectClient
{
    public static ChronicleClient Create() =>
        new(options: new ChronicleOptions(), namingPolicy: new CamelCaseNamingPolicy());
}
```
