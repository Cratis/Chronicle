```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspireMongo
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        var mongo = builder.AddConnectionString("chronicle-mongo");

        return builder.AddCratisChronicle("chronicle", c =>
            c.WithMongoDB(mongo));
    }
}
```
