```csharp
public static class HostingLocalCertificatesClientConfiguration
{
    public static ChronicleClient Create()
    {
        var options = new ChronicleOptions
        {
            ConnectionString = "chronicle://localhost:35000",
            Tls = new Tls
            {
                CertificatePath = "./chronicle-dev.pfx",
                CertificatePassword = "YourPassword123"
            }
        };

        return new ChronicleClient(options);
    }
}
```
