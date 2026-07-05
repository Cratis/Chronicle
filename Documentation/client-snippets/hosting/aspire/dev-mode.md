```csharp
using Aspire.Hosting;

public static class HostingAspireDevMode
{
    public static void ConfigureAppHost(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var chronicle = builder.AddCratisChronicle();

        builder.AddContainer("api", "my-org/my-api")
            .WithReference(chronicle);

        builder.Build().Run();
    }
}
```
