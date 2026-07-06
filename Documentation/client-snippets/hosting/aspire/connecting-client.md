```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspireConnectingClient
{
    public static void ConfigureAppHost(IDistributedApplicationBuilder builder, IResourceBuilder<ChronicleResource> chronicle)
    {
        // Reference any of your application's projects the same way -
        // builder.AddProject<Projects.MyApi>("api") when Projects.MyApi is generated from your solution
        builder.AddContainer("api", "my-org/my-api")
            .WithReference(chronicle);
    }
}
```
