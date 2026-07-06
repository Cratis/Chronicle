```csharp
using Microsoft.Extensions.Hosting;

public static class ConfigurationIndexRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(options => options.EventStore = "my-store");
    }
}
```
