```csharp
using Microsoft.Extensions.Hosting;

public static class ConnectionStringsConfigurationRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(options => options.EventStore = "my-store");
    }
}
```
