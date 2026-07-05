```csharp
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class SubscriptionsExplicitTypicalPattern
{
    public static async Task RegisterSubscriptions(IEventStore eventStore)
    {
        await eventStore.Subscriptions.Subscribe(
            "orders-from-fulfillment",
            "fulfillment-service",
            builder => builder.WithEventType<SubscriptionsExplicitShipmentDispatched>());

        await eventStore.Subscriptions.Subscribe(
            "inventory-updates",
            "warehouse-service",
            builder => builder
                .WithEventType<SubscriptionsExplicitStockAdjusted>()
                .WithEventType<SubscriptionsExplicitStockReserved>());
    }

    public static async Task Configure(string[] args)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);
        hostBuilder.AddCratisChronicle(options => options.EventStore = "Quickstart");

        var app = hostBuilder.Build();
        var eventStore = app.Services.GetRequiredService<IEventStore>();
        await RegisterSubscriptions(eventStore);
        await app.RunAsync();
    }
}
```
