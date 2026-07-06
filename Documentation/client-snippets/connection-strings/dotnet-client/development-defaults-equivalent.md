```csharp
using Cratis.Chronicle.Connections;

public static class ConnectionStringsDevelopmentDefaultsEquivalent
{
    public static ChronicleClient CreateFromOptions()
    {
        var options = ChronicleOptions.FromDevelopmentConnectionString();
        return new ChronicleClient(options);
    }

    public static ChronicleClient CreateFromConnectionString() => new(ChronicleConnectionString.Development);
}
```
