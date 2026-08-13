```csharp
public static class ConnectionStringsFromConnectionString
{
    public static ChronicleClient Create()
    {
        var options = ChronicleOptions.FromConnectionString("chronicle://localhost:35000");
        return new ChronicleClient(options);
    }
}
```
