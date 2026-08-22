```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspirePinImage
{
    public static IResourceBuilder<ChronicleResource> Configure(
        IDistributedApplicationBuilder builder) =>
        builder
            .AddCratisChronicle(configure: chronicle => chronicle
                .WithTlsCertificate("certs/chronicle.pfx", "YourPassword")
                .WithEncryptionCertificate(
                    "certs/encryption.pfx",
                    "YourPassword"))
            .WithImageTag("16.26.0");
}
```
