```csharp
using Microsoft.Extensions.Hosting;

public static class ChronicleOptionsConnectionStringRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(configureOptions: options =>
        {
            options.ConnectionString = "chronicle://myserver:35000";
        });
    }
}
```
