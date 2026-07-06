```csharp
public static class TlsClientOptions
{
    public static ChronicleClient Create()
    {
        var options = new ChronicleOptions
        {
            ConnectionString = "chronicle://localhost:35000",
            Tls = new Tls
            {
                CertificatePath = "/path/to/certificate.pfx",
                CertificatePassword = "your-password"
            }
        };

        return new ChronicleClient(options);
    }
}
```
