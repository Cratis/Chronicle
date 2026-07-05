```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class GetStartedHostingSqlServerDatabase
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        // Use the Aspire.Hosting.SqlServer integration's builder.AddSqlServer(...).AddDatabase(...) when
        // your AppHost already provisions the database — any resource with a connection string works here.
        var sql = builder.AddConnectionString("chronicle-sql");

        return builder.AddCratisChronicle("chronicle", c => c.WithMsSql(sql));
    }
}
```
