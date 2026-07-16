```csharp
public static class TlsConnectionStringSkipValidation
{
    public static ChronicleOptions Create() =>
        ChronicleOptions.FromConnectionString("chronicle://localhost:35000?skipTlsValidation=true");
}
```
