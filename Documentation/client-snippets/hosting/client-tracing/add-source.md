```csharp
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

public static class ClientTracingAddSourceRegistration
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle();

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(ClientActivity.SourceName)
                .AddOtlpExporter());
    }
}
```
