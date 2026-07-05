```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspirePostgres
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        var postgres = builder.AddConnectionString("chronicle-postgres");

        return builder.AddCratisChronicle("chronicle", c =>
            c.WithPostgreSql(postgres));
    }
}
```
