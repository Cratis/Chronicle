```csharp
using Cratis.Chronicle.AspNetCore.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class ClientTracingRegistration
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddCratisChronicle();

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("orders-api"))
            .WithTracing(tracing => tracing
                .AddCratisChronicleInstrumentation()
                .AddOtlpExporter());

        var app = builder.Build();
    }
}
```
