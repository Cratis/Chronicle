```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspireSqlServer
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        var sql = builder.AddConnectionString("chronicle-sql");

        return builder.AddCratisChronicle("chronicle", c =>
            c.WithMsSql(sql));
    }
}
```
