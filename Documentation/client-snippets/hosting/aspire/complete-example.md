```csharp
using Aspire.Hosting;

public static class HostingAspireCompleteExample
{
    public static void ConfigureAppHost(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var mongo = builder.AddConnectionString("chronicle-mongo");

        var chronicle = builder.AddCratisChronicle("chronicle", c => c
            .WithMongoDB(mongo)
            .WithTlsCertificate("certs/chronicle.pfx", "YourPassword")
            .WithEncryptionCertificate("certs/encryption.pfx", "YourPassword"));

        builder.AddContainer("api", "my-org/my-api")
            .WithReference(chronicle);

        builder.Build().Run();
    }
}
```
