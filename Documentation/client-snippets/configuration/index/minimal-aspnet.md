```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

public static class MinimalChronicleWebApplication
{
    public static async Task Run(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddCratisChronicle();
        builder.Services.Configure<ChronicleAspNetCoreOptions>(options => options.UseAutomaticMiddleware = false);
        var app = builder.Build();
        app.UseRouting();
        app.UseCratisChronicle();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        await app.RunAsync();
    }
}
```
