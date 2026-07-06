```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class GetStartedHostingPostgresDatabase
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        // Use the Aspire.Hosting.PostgreSQL integration's builder.AddPostgres(...).AddDatabase(...) when
        // your AppHost already provisions the database — any resource with a connection string works here.
        var postgres = builder.AddConnectionString("chronicle-postgres");

        return builder.AddCratisChronicle("chronicle", c => c.WithPostgreSql(postgres));
    }
}
```
