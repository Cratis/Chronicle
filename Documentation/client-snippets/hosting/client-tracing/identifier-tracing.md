```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var hashKey = builder.Configuration["ChronicleSecrets:EventSourceIdHashKey"];

builder.AddCratisChronicle(configure: chronicle => chronicle.WithTelemetry(options =>
{
    options.EventSourceIdTracing = EventSourceIdTracing.Hashed;
    options.EventSourceIdHashKey = hashKey;
}));

await builder.Build().RunAsync();
```
