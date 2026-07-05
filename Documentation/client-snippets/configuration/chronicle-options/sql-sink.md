```csharp
using Microsoft.Extensions.Hosting;
using Cratis.Chronicle.Sinks;

public static class ChronicleOptionsSqlSinkRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(configureOptions: options =>
        {
            options.DefaultSinkTypeId = WellKnownSinkTypes.SQL;
        });
    }
}
```
