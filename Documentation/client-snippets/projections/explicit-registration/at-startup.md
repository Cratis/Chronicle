```csharp
using Microsoft.Extensions.Hosting;

public static class ExplicitProjectionsAtStartup
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(configureOptions: options => options.ExplicitArtifacts
            .RegisterProjection<WarehouseStock>(projection => projection
                .From<StockReceived>(from => from.Add(model => model.Quantity).With(@event => @event.Quantity))
                .From<StockPicked>(from => from.Subtract(model => model.Quantity).With(@event => @event.Quantity)))
            .RegisterReadModel<Supplier>());
    }
}
```
