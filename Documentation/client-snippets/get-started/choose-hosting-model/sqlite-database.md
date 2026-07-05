```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class GetStartedHostingSqliteDatabase
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder) =>
        builder.AddCratisChronicle("chronicle", c => c.WithSqlite("Data Source=/data/chronicle.db"));
}
```
