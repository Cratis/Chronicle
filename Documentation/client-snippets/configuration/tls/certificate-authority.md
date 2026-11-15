```csharp
public static class ConfigurationTlsCertificateAuthority
{
    public static ChronicleOptions Create() =>
        ChronicleOptions.FromConnectionString(
            "chronicle://my-server:35000?skipTlsValidation=false&caCertificatePath=/certs/ca.pem");
}
```
