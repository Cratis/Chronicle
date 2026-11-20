```csharp
using Microsoft.Extensions.Hosting;

public static class MinimalChronicleWorker
{
    public static async Task Run(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddCratisChronicle();
        using var host = builder.Build();
        await host.RunAsync();
    }
}
```
