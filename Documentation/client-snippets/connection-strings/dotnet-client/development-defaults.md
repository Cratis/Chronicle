```csharp
public static class ConnectionStringsDevelopmentDefaults
{
    public static ChronicleClient Create()
    {
        var options = new ChronicleOptions();
        return new ChronicleClient(options);
    }
}
```
