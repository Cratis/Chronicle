```csharp
using Microsoft.Extensions.Hosting;

public static class GetStartedWorkerRegistration
{
    public static async Task ConfigureAndRun(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(options => options.EventStore = "Quickstart");
        builder.Services.AddHostedService<GetStartedWorker>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
```
