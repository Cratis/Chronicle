```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspireCertificates
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        var mongo = builder.AddConnectionString("chronicle-mongo");

        return builder.AddCratisChronicle("chronicle", chronicle => chronicle
            .WithMongoDB(mongo)
            .WithTlsCertificate("certs/chronicle.pfx", "YourPassword")
            .WithEncryptionCertificate("certs/encryption.pfx", "YourPassword"));
    }
}
```
