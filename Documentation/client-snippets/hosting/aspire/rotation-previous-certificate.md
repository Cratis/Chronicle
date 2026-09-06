```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspireRotationPreviousCertificate
{
    public static IResourceBuilder<ChronicleResource> Configure(
        IDistributedApplicationBuilder builder) =>
        builder
            .AddCratisChronicle(configure: chronicle => chronicle
                .WithTlsCertificate("certs/chronicle.pfx", "YourPassword")
                .WithEncryptionCertificate(
                    "certs/encryption-2026.pfx",
                    "YourNewPassword"))
            .WithBindMount(
                "certs/encryption-2025.pfx",
                "/certs/encryption-previous.pfx",
                isReadOnly: true)
            .WithEnvironment(
                "Cratis__Chronicle__EncryptionCertificate__Previous__0__CertificatePath",
                "/certs/encryption-previous.pfx")
            .WithEnvironment(
                "Cratis__Chronicle__EncryptionCertificate__Previous__0__CertificatePassword",
                "YourOldPassword");
}
```
