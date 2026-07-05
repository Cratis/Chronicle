```csharp
public static class TlsConnectionStringDisable
{
    public static ChronicleOptions Create() =>
        ChronicleOptions.FromConnectionString("chronicle://localhost:35000?disableTls=true");
}
```
