```csharp
using Aspire.Hosting;

public static class GetStartedHostingBasicAppHost
{
    public static void ConfigureAppHost(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var chronicle = builder.AddCratisChronicle();

        // Reference any of your application's projects the same way -
        // builder.AddProject<Projects.MyApi>("api") when Projects.MyApi is generated from your solution
        builder.AddContainer("api", "my-org/my-api")
            .WithReference(chronicle);

        builder.Build().Run();
    }
}
```
