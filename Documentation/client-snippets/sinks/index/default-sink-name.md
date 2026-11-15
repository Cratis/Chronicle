```csharp
using Microsoft.Extensions.Hosting;

public static class SinksDefaultSinkName
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(configureOptions: options =>
        {
            options.DefaultSinkName = "reporting";
            options.DefaultGraphSinkName = "people";
        });
    }
}
```
