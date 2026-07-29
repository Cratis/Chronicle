```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspireMongoReplicaSetHelper
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        var mongo = builder.AddCratisChronicleMongoDB();

        return builder.AddCratisChronicle(configure: chronicle => chronicle.WithMongoDB(mongo));
    }
}
```
