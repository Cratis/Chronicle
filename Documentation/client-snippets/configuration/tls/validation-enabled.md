```csharp
public static class ConfigurationTlsValidationEnabled
{
    public static ChronicleOptions Create() =>
        ChronicleOptions.FromConnectionString(
            "chronicle://my-server:35000?skipTlsValidation=false");
}
```
